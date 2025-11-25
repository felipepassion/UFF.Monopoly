using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore; // corrigido namespace EF Core
using Microsoft.AspNetCore.Components.Web;
using UFF.Monopoly.Data;
using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Entities;
using UFF.Monopoly.Models;

namespace UFF.Monopoly.Components.Pages.BoardBuilders;

public partial class BoardBuilder : ComponentBase
{
    [Parameter] public Guid? BoardId { get; set; }
    [SupplyParameterFromQuery] public bool Saved { get; set; }

    [Inject] public IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private bool _savedToastShown;
    private Guid CurrentBoardId;
    internal BoardFormModel Form = new();
    internal List<BlockTemplateEntity> Blocks = new();
    internal List<BoardDefinitionEntity> ExistingBoards = new();
    internal Dictionary<(int r, int c), int> CellToIndex = new();
    internal (bool Visible, int X, int Y, int Index) Popup;
    internal (bool Visible, int Index) CategoryModal = (false, -1);
    internal (bool Show, int Index, BlockType? Type) _specialModal = (false, -1, null);
    internal bool ShowToast; internal string ToastMessage = string.Empty;

    // Cache de preços editados em memória até F5
    private readonly Dictionary<(Guid blockId, BuildingType cat, int level), int> _priceCache = new();

    internal int GetCachedPrice(BlockTemplateEntity? block, BuildingType cat, int level)
    {
        if (block is null) return 0;
        if (_priceCache.TryGetValue((block.Id, cat, level), out var cached)) return cached;
        // fallback: extrair do CSV do bloco
        if (block.BuildingType == cat && !string.IsNullOrWhiteSpace(block.BuildingPricesCsv))
        {
            var prices = block.BuildingPricesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => int.TryParse(p, out var v) ? v : 0).ToArray();
            var idx = Math.Clamp(level - 1, 0, prices.Length - 1);
            if (idx < prices.Length) return prices[idx];
        }
        var defaults = GetDefaultPricesForCategory(cat);
        return defaults[Math.Clamp(level - 1, 0, defaults.Length - 1)];
    }

    internal void SetCachedPrice(BlockTemplateEntity? block, BuildingType cat, int level, int price)
    {
        if (block is null) return;
        price = Math.Max(0, price);
        _priceCache[(block.Id, cat, level)] = price;
        // Atualiza CSV somente desta instância, mantendo persistência ao salvar manualmente
        int[] prices;
        if (!string.IsNullOrWhiteSpace(block.BuildingPricesCsv))
            prices = block.BuildingPricesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => int.TryParse(p, out var v) ? v : 0).ToArray();
        else
            prices = GetDefaultPricesForCategory(cat).ToArray();
        if (prices.Length < 4) Array.Resize(ref prices, 4);
        prices[Math.Clamp(level - 1, 0, 3)] = price;
        block.BuildingPricesCsv = string.Join(',', prices);
        if (block.BuildingType == cat)
        {
            block.Price = price;
            if (block.Rent <= 0) block.Rent = (int)Math.Max(1, Math.Round(block.Price * 0.08));
        }
    }

    protected override async Task OnInitializedAsync() => await LoadBoardsListAsync();

    protected override async Task OnParametersSetAsync()
    { if (BoardId.HasValue && BoardId.Value != Guid.Empty && BoardId.Value != CurrentBoardId) { await LoadBoardAsync(BoardId.Value); } if (Saved && !_savedToastShown) { _savedToastShown = true; _ = ShowSuccessToast("Salvo com sucesso!"); } }

    private async Task LoadBoardsListAsync()
    { await using var db = await DbFactory.CreateDbContextAsync(); ExistingBoards = await db.Boards.AsNoTracking().OrderByDescending(b => b.CreatedAt).ToListAsync(); }

    private async Task LoadBoardAsync(Guid id)
    { await using var db = await DbFactory.CreateDbContextAsync(); var board = await db.Boards.Include(b => b.Blocks).FirstOrDefaultAsync(b => b.Id == id); if (board is null) return; CurrentBoardId = id; Form.Name = board.Name; Form.Rows = board.Rows; Form.Cols = board.Cols; Form.CellSize = board.CellSizePx; Form.CenterImageUrl = board.CenterImageUrl ?? Form.CenterImageUrl; Blocks = board.Blocks.OrderBy(b => b.Position).ToList(); RebuildPerimeterMap(); StateHasChanged(); }

    internal void NewBoard() { CurrentBoardId = Guid.Empty; Form = new BoardFormModel(); Blocks = new(); CellToIndex.Clear(); StateHasChanged(); }
    internal void EditBoard(Guid id) => Nav.NavigateTo($"/board-builder/{id}");

    internal void GenerateGrid()
    { Form.Rows = Math.Max(2, Form.Rows); Form.Cols = Math.Max(2, Form.Cols); RebuildPerimeterMap(); var total = CellToIndex.Count; Blocks = Enumerable.Range(0, total).Select(i => new BlockTemplateEntity { Id = Guid.NewGuid(), Position = i, Name = $"Bloco {i}", Type = BlockType.Property, Color = GetTypeColor(BlockType.Property), ImageUrl = GetDefaultImageForType(BlockType.Property), Price = 0, Rent = 0, BoardDefinitionId = CurrentBoardId, BuildingType = BuildingType.House, BuildingPricesCsv = string.Join(',', GetDefaultPricesForCategory(BuildingType.House)) }).ToList(); StateHasChanged(); }

    private void RebuildPerimeterMap()
    { var coords = BuildPerimeterClockwise(Form.Rows, Form.Cols); CellToIndex = new Dictionary<(int r, int c), int>(coords.Count); for (int i = 0; i < coords.Count; i++) CellToIndex[(coords[i].r, coords[i].c)] = i; }

    private static List<(int r, int c)> BuildPerimeterClockwise(int rows, int cols)
    { var list = new List<(int r, int c)>(Math.Max(0, 2 * rows + 2 * cols - 4)); if (rows < 2 || cols < 2) return list; int bottom = rows - 1; int top = 0; int left = 0; int right = cols - 1; for (int c = right; c >= left; c--) list.Add((bottom, c)); for (int r = bottom - 1; r >= top; r--) list.Add((r, left)); for (int c = left + 1; c <= right; c++) list.Add((top, c)); for (int r = top + 1; r <= bottom - 1; r++) list.Add((r, right)); return list; }

    internal void OnCellClick(int index, MouseEventArgs e) { Popup = (true, (int)e.ClientX, (int)e.ClientY, index); StateHasChanged(); }
    internal void ClosePopup() { Popup.Visible = false; StateHasChanged(); }

    internal BlockTemplateEntity? GetBlockAt(int index) => (index >= 0 && index < Blocks.Count) ? Blocks[index] : null;

    internal void SelectType(BlockType type)
    {
        var idx = Popup.Index;
        if (idx < 0 || idx >= Blocks.Count) { Popup.Visible = false; return; }
        if (type == BlockType.Property || type == BlockType.Company) { CategoryModal = (true, idx); Popup.Visible = false; StateHasChanged(); return; }
        // Tipos especiais que precisam de valor
        if (type is BlockType.Tax or BlockType.Chance or BlockType.Reves or BlockType.GoToJail)
        {
            _specialModal = (true, idx, type);
            Popup.Visible = false; StateHasChanged();
            return;
        }
        Blocks[idx].Type = type;
        Blocks[idx].Color = GetTypeColor(type);
        Blocks[idx].ImageUrl = GetDefaultImageForType(type);
        Blocks[idx].Name = TranslateBlockType(type);
        Popup.Visible = false; StateHasChanged();
    }

    private void HideSpecialModal() { _specialModal = (false, -1, null); StateHasChanged(); }
    private void OnApplySpecial(BlockTemplateEntity b)
    {
        var idx = _specialModal.Index; if (idx >= 0 && idx < Blocks.Count)
        {
            Blocks[idx] = b;
            Blocks[idx].Type = _specialModal.Type ?? Blocks[idx].Type;
            Blocks[idx].Color = GetTypeColor(Blocks[idx].Type);
            Blocks[idx].ImageUrl = GetDefaultImageForType(Blocks[idx].Type);
            if (Blocks[idx].Type == BlockType.Tax) Blocks[idx].Name = "Taxa";
            else if (Blocks[idx].Type == BlockType.Chance) Blocks[idx].Name = "Sorte";
            else if (Blocks[idx].Type == BlockType.Reves) Blocks[idx].Name = "Revés";
            else if (Blocks[idx].Type == BlockType.GoToJail) Blocks[idx].Name = "Vá para Prisão";
        }
        _specialModal = (false, -1, null); StateHasChanged();
    }

    internal void CloseCategoryModal() { CategoryModal = (false, -1); StateHasChanged(); }

    internal void ApplyCategory(BuildingType cat, int level)
    {
        var idx = CategoryModal.Index;
        if (idx < 0 || idx >= Blocks.Count) { CloseCategoryModal(); return; }
        var block = Blocks[idx];
        block.Type = (cat == BuildingType.Company) ? BlockType.Company : BlockType.Property;
        block.BuildingType = cat;
        block.ImageUrl = GetBuildingImage(cat, level);
        var prices = GetDefaultPricesForCategory(cat);
        block.BuildingPricesCsv = string.Join(',', prices);
        var evo = BuildingEvolutionDescriptions.Get(cat, level);
        block.Name = evo.Name;
        // Atualiza preço conforme nível escolhido (usa custo incremental daquele nível)
        if (level >= 1 && level <= prices.Length)
        {
            block.Price = prices[level - 1];
            // Opcional: ajustar aluguel base se ainda zero (regra simples proporcional)
            if (block.Rent <= 0) block.Rent = (int)Math.Max(1, Math.Round(block.Price * 0.08));
        }
        var wasGeneric = string.IsNullOrWhiteSpace(block.Description) || block.Description.StartsWith("Bloco", StringComparison.OrdinalIgnoreCase) || block.Description.Equals(block.Name, StringComparison.OrdinalIgnoreCase);
        if (wasGeneric) { block.Description = evo.Description; }
        else if (!block.Description.Contains(evo.Name, StringComparison.OrdinalIgnoreCase)) { block.Description += $" (Nível: {evo.Name})"; }
        CloseCategoryModal(); StateHasChanged();
    }

    internal void ApplyCategoryWithPrice(BuildingType cat, int level, int customPrice)
    {
        var idx = CategoryModal.Index;
        if (idx < 0 || idx >= Blocks.Count) { CloseCategoryModal(); return; }
        var block = Blocks[idx];
        block.Type = (cat == BuildingType.Company) ? BlockType.Company : BlockType.Property;
        block.BuildingType = cat;
        block.ImageUrl = GetBuildingImage(cat, level);
        var prices = GetDefaultPricesForCategory(cat);
        block.BuildingPricesCsv = string.Join(',', prices);
        var evo = BuildingEvolutionDescriptions.Get(cat, level);
        block.Name = evo.Name;
        block.Price = Math.Max(0, customPrice);
        block.Rent = (int)Math.Max(1, Math.Round(block.Price * 0.08));
        var wasGeneric = string.IsNullOrWhiteSpace(block.Description) || block.Description.StartsWith("Bloco", StringComparison.OrdinalIgnoreCase) || block.Description.Equals(block.Name, StringComparison.OrdinalIgnoreCase);
        if (wasGeneric) { block.Description = evo.Description; }
        else if (!block.Description.Contains(evo.Name, StringComparison.OrdinalIgnoreCase)) { block.Description += $" (Nível: {evo.Name})"; }
        CloseCategoryModal(); StateHasChanged();
    }

    // Seleção rápida acionada pelos atalhos ou clique direto de nível no popup
    internal void QuickPropertySelect(BuildingType cat, int level)
    {
        var idx = Popup.Index;
        if (idx < 0 || idx >= Blocks.Count) { Popup.Visible = false; return; }
        var b = Blocks[idx];

        // Reconstroi lista de preços considerando cache e CSV existente
        int[] defaultPrices = GetDefaultPricesForCategory(cat);
        int[] existingPrices = (!string.IsNullOrWhiteSpace(b.BuildingPricesCsv)) ?
            b.BuildingPricesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => int.TryParse(p, out var v) ? v : 0).ToArray() :
            Array.Empty<int>();
        if (existingPrices.Length < 4) Array.Resize(ref existingPrices, 4);

        int[] finalPrices = new int[4];
        for (int i = 1; i <= 4; i++)
        {
            // Ordem de prioridade: cache -> existing CSV -> default
            if (_priceCache.TryGetValue((b.Id, cat, i), out var cached)) finalPrices[i - 1] = cached;
            else if (existingPrices[i - 1] > 0) finalPrices[i - 1] = existingPrices[i - 1];
            else finalPrices[i - 1] = defaultPrices[Math.Clamp(i - 1, 0, defaultPrices.Length - 1)];
        }
        b.BuildingPricesCsv = string.Join(',', finalPrices);

        var evo = BuildingEvolutionDescriptions.Get(cat, level);
        b.Type = (cat == BuildingType.Company) ? BlockType.Company : BlockType.Property;
        b.BuildingType = cat;
        b.ImageUrl = GetBuildingImage(cat, level);
        b.Name = evo.Name;

        // Usa preço da posição escolhida considerando cache
        b.Price = finalPrices[Math.Clamp(level - 1, 0, finalPrices.Length - 1)];
        b.Rent = (int)Math.Max(1, Math.Round(b.Price * (cat == BuildingType.Company ? 0.10 : 0.08)));

        if (string.IsNullOrWhiteSpace(b.Description) || b.Description.StartsWith("Bloco", StringComparison.OrdinalIgnoreCase))
            b.Description = evo.Description;

        Popup.Visible = false; StateHasChanged();
    }

    // Abre modal completo de categoria a partir do botão "Ver detalhes" no popup
    internal void ShowDetailsFromPopup()
    { CategoryModal = (true, Popup.Index); Popup.Visible = false; StateHasChanged(); }

    internal string GetCellBackground(BlockTemplateEntity b) => string.IsNullOrWhiteSpace(b.Color) ? GetTypeColor(b.Type) : b.Color;

    internal string GetTypeColor(BlockType t) => t switch { BlockType.Go => "#27ae60", BlockType.Property => "#3498db", BlockType.Company => "#2ecc71", BlockType.Tax => "#e74c3c", BlockType.Jail => "#f1c40f", BlockType.GoToJail => "#d35400", BlockType.Chance => "#8e44ad", BlockType.Reves => "#f39c12", BlockType.FreeParking => "#16a085", _ => "#95a5a6" };

    internal string GetDefaultImageForType(BlockType t) => t switch { BlockType.Property => "/images/board/buildings/house1.png", BlockType.Company => "/images/board/buildings/company1.png", BlockType.GoToJail => "/images/blocks/go_to_jail.svg", BlockType.Jail => "/images/blocks/visitar_prisao.svg", BlockType.Tax => "/images/blocks/taxa.png", BlockType.Go => "/images/blocks/volte-casas.svg", BlockType.Chance => "/images/blocks/sorte.png", BlockType.Reves => "/images/blocks/reves.png", BlockType.FreeParking => "/images/board/blocks/freeparking.png", _ => "/images/board/buildings/house1.png" };

    internal string GetCategoryName(BuildingType cat) => cat switch { BuildingType.House => "Casas", BuildingType.Hotel => "Hotéis", BuildingType.Company => "Empresas", BuildingType.Special => "Prédios Especiais", _ => cat.ToString() };
    internal string GetCategoryColor(BuildingType cat) => cat switch { BuildingType.House => GetTypeColor(BlockType.Property), BuildingType.Hotel => GetTypeColor(BlockType.Property), BuildingType.Company => GetTypeColor(BlockType.Company), BuildingType.Special => GetTypeColor(BlockType.FreeParking), _ => "#95a5a6" };
    internal static int[] GetDefaultPricesForCategory(BuildingType cat) => cat switch { BuildingType.House => new[] { 100, 200, 350, 500 }, BuildingType.Hotel => new[] { 300, 600, 1000, 1500 }, BuildingType.Company => new[] { 400, 800, 1300, 1900 }, BuildingType.Special => new[] { 500, 1000, 1600, 2300 }, _ => new[] { 0, 0, 0, 0 } };
    internal static string GetBuildingImage(BuildingType cat, int level) => cat switch { BuildingType.House => $"/images/board/buildings/house{Math.Clamp(level, 1, 4)}.png", BuildingType.Hotel => $"/images/board/buildings/hotel{Math.Clamp(level, 1, 4)}.png", BuildingType.Company => $"/images/board/buildings/company{Math.Clamp(level, 1, 4)}.png", BuildingType.Special => Math.Clamp(level, 1, 4) switch { 1 => "/images/board/buildings/special_circus.png", 2 => "/images/board/buildings/special_shopping.png", 3 => "/images/board/buildings/special_stadium.png", _ => "/images/board/buildings/special_airport.png" }, _ => "/images/board/buildings/house1.png" };
    internal static string GetBuildingName(BuildingType cat, int level) => BuildingEvolutionDescriptions.Get(cat, level).Name;

    internal async Task SaveAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var isNew = CurrentBoardId == Guid.Empty;
        if (isNew)
        {
            var newBoard = new BoardDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrWhiteSpace(Form.Name) ? $"Board {DateTime.UtcNow:yyyyMMddHHmmss}" : Form.Name.Trim(),
                Rows = Form.Rows,
                Cols = Form.Cols,
                CellSizePx = Form.CellSize,
                CenterImageUrl = Form.CenterImageUrl
            };
            db.Boards.Add(newBoard);
            await db.SaveChangesAsync();
            CurrentBoardId = newBoard.Id;
        }
        else
        {
            var boardOnly = new BoardDefinitionEntity { Id = CurrentBoardId };
            db.Attach(boardOnly);
            db.Entry(boardOnly).Property(x => x.Name).CurrentValue = Form.Name.Trim();
            db.Entry(boardOnly).Property(x => x.Rows).CurrentValue = Form.Rows;
            db.Entry(boardOnly).Property(x => x.Cols).CurrentValue = Form.Cols;
            db.Entry(boardOnly).Property(x => x.CellSizePx).CurrentValue = Form.CellSize;
            db.Entry(boardOnly).Property(x => x.CenterImageUrl).CurrentValue = Form.CenterImageUrl;
            db.Entry(boardOnly).Property(x => x.Name).IsModified = true;
            db.Entry(boardOnly).Property(x => x.Rows).IsModified = true;
            db.Entry(boardOnly).Property(x => x.Cols).IsModified = true;
            db.Entry(boardOnly).Property(x => x.CellSizePx).IsModified = true;
            db.Entry(boardOnly).Property(x => x.CenterImageUrl).IsModified = true;
            await db.SaveChangesAsync();

            var existing = await db.BlockTemplates.Where(x => x.BoardDefinitionId == CurrentBoardId).ToListAsync();
            if (existing.Count > 0)
            {
                db.BlockTemplates.RemoveRange(existing);
                await db.SaveChangesAsync();
            }
        }

        var toInsert = new List<BlockTemplateEntity>(Blocks.Count);
        for (var i = 0; i < Blocks.Count; i++)
        {
            var b = Blocks[i];
            toInsert.Add(new BlockTemplateEntity
            {
                Id = Guid.NewGuid(),
                Position = i,
                Name = b.Name,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                Color = b.Color,
                Price = b.Price,
                Rent = b.Rent,
                Type = b.Type,
                Level = b.Level,
                HousePrice = b.HousePrice,
                HotelPrice = b.HotelPrice,
                RentsCsv = b.RentsCsv,
                BuildingType = b.BuildingType,
                BuildingPricesCsv = b.BuildingPricesCsv,
                BoardDefinitionId = CurrentBoardId
            });
        }
        db.BlockTemplates.AddRange(toInsert);
        await db.SaveChangesAsync();
        await LoadBoardsListAsync();
        if (isNew) { Nav.NavigateTo($"/board-builder/{CurrentBoardId}?saved=true"); }
        else { await ShowSuccessToast("Salvo com sucesso!"); }
        StateHasChanged();
    }

    internal async Task ShowSuccessToast(string message) { ToastMessage = message; ShowToast = true; StateHasChanged(); try { await Task.Delay(2000); } catch { } ShowToast = false; StateHasChanged(); }

    internal void PlayBoard()
    {
        if (CurrentBoardId == Guid.Empty)
        {
            _ = ShowSuccessToast("Salve o tabuleiro antes de jogar!");
            return;
        }
        Nav.NavigateTo($"/play/{CurrentBoardId}");
    }

    internal string TranslateBlockType(BlockType t) => t switch { BlockType.Go => "Início", BlockType.Property => "Propriedade", BlockType.Company => "Companhia", BlockType.Tax => "Taxa", BlockType.Jail => "Visitar Prisão", BlockType.GoToJail => "Vá para Prisão", BlockType.Chance => "Sorte", BlockType.Reves => "Revés", BlockType.FreeParking => "Parada Livre", _ => t.ToString() };

    // === Random board generation with basic balance rules ===
    internal void GenerateRandomBoard()
    {
        // Não alterar o tamanho do board; apenas garantir mínimos.
        var rng = Random.Shared;
        if (Form.Rows < 3) Form.Rows = 3;
        if (Form.Cols < 3) Form.Cols = 3;
        if (Form.CellSize <= 0) Form.CellSize = 56;

        // Mantém o tamanho existente sem aplicar lógica de widescreen ou randomização.
        RebuildPerimeterMap();
        var total = CellToIndex.Count; // perimeter cells count
        if (total < 8) { GenerateGrid(); return; }

        Blocks = Enumerable.Range(0, total).Select(i => new BlockTemplateEntity
        {
            Id = Guid.NewGuid(),
            Position = i,
            Name = $"Bloco {i}",
            Type = BlockType.Property,
            Color = GetTypeColor(BlockType.Property),
            ImageUrl = GetDefaultImageForType(BlockType.Property),
            Price = 0,
            Rent = 0,
            BoardDefinitionId = CurrentBoardId,
            BuildingType = BuildingType.House,
            BuildingPricesCsv = string.Join(',', GetDefaultPricesForCategory(BuildingType.House))
        }).ToList();

        // Place fixed corners like Monopoly
        int start = 0;
        int leftBottom = (Form.Cols - 1) - 0;
        int topLeft = leftBottom + (Form.Rows - 1);
        int topRight = topLeft + (Form.Cols - 1);
        int bottomRight = total - 1;

        start = 0;
        var corners = new[] { start, leftBottom, topLeft, bottomRight };

        void SetBlock(int idx, BlockType t, string name, int defaultValue = 0)
        {
            var b = Blocks[idx];
            b.Type = t;
            b.Color = GetTypeColor(t);
            b.ImageUrl = GetDefaultImageForType(t);
            b.Name = name;
            b.BuildingType = BuildingType.None;
            b.Price = 0; b.Rent = defaultValue; b.BuildingPricesCsv = null; b.Level = null;
        }

        SetBlock(corners[0], BlockType.Go, "Início");
        SetBlock(corners[1], BlockType.Jail, "Visitar Prisão", 0);
        SetBlock(corners[2], BlockType.FreeParking, "Parada Livre", 0);
        SetBlock(corners[3], BlockType.GoToJail, "Vá para Prisão", 3); // default turns

        int specialsBudget(int min, int max) => Math.Clamp((int)Math.Round(total * 0.08), min, max);
        int chanceCount = specialsBudget(2, 4);
        int revesCount = specialsBudget(2, 4);
        int taxCount = Math.Clamp((int)Math.Round(total * 0.06), 1, 3);
        int goToJailExtra = 0;
        int companyCount = Math.Clamp((int)Math.Round(total * 0.12), 2, Math.Max(2, total / 6));

        var reserved = new HashSet<int>(corners);

        bool IsValidSpot(int idx)
        {
            if (reserved.Contains(idx)) return false;
            foreach (var c in corners)
                if (Math.Abs(idx - c) <= 1) return false;
            return true;
        }

        IEnumerable<int> FreeIndices() => Enumerable.Range(0, total).Where(i => !reserved.Contains(i));

        void PlaceMany(BlockType t, int count, string defaultName, int defaultValue)
        {
            int placed = 0; int safety = 0;
            while (placed < count && safety++ < total * 5)
            {
                var idx = FreeIndices().OrderBy(_ => rng.Next()).FirstOrDefault();
                if (idx == 0 && reserved.Contains(idx)) break;
                if (!IsValidSpot(idx)) continue;
                var b = Blocks[idx];
                b.Type = t; b.Color = GetTypeColor(t); b.ImageUrl = GetDefaultImageForType(t); b.Name = defaultName;
                b.BuildingType = BuildingType.None; b.Price = 0; b.Rent = defaultValue; b.BuildingPricesCsv = null; b.Level = null;
                reserved.Add(idx); placed++;
            }
        }

        PlaceMany(BlockType.Chance, chanceCount, "Sorte", 2); // default positive move/value
        PlaceMany(BlockType.Reves, revesCount, "Revés", 2); // default backward steps
        PlaceMany(BlockType.Tax, taxCount, "Taxa", 150); // default tax value
        if (goToJailExtra > 0) PlaceMany(BlockType.GoToJail, goToJailExtra, "Vá para Prisão", 3);

        void PlaceCompanies(int count)
        {
            int placed = 0; int safety = 0;
            while (placed < count && safety++ < total * 5)
            {
                var idx = FreeIndices().OrderBy(_ => rng.Next()).FirstOrDefault();
                if (!IsValidSpot(idx)) continue;
                bool nearCompany = reserved.Any(r => Math.Abs(r - idx) <= 2 && Blocks[r].Type == BlockType.Company);
                if (nearCompany) continue;
                var level = rng.Next(1, 5);
                var prices = GetDefaultPricesForCategory(BuildingType.Company);
                var b = Blocks[idx];
                b.Type = BlockType.Company; b.Color = GetTypeColor(BlockType.Company); b.ImageUrl = GetBuildingImage(BuildingType.Company, level);
                b.Name = BuildingEvolutionDescriptions.Get(BuildingType.Company, level).Name;
                b.BuildingType = BuildingType.Company; b.BuildingPricesCsv = string.Join(',', prices);
                b.Price = prices[level - 1]; b.Rent = (int)Math.Max(1, Math.Round(b.Price * 0.1));
                reserved.Add(idx); placed++;
            }
        }
        PlaceCompanies(companyCount);

        var remaining = FreeIndices().ToList();
        remaining.Sort();
        int groups = Math.Clamp(total / 10, 4, 8);
        var colorPalette = new[] { "#3498db", "#e67e22", "#9b59b6", "#1abc9c", "#e74c3c", "#2ecc71", "#34495e", "#f39c12" };

        int idxInGroup = 0; int group = 0; int basePrice = 60; int priceStep = Math.Max(10, total);
        foreach (var i in remaining)
        {
            var level = 1 + (group % 4);
            var cat = BuildingType.House;
            var prices = GetDefaultPricesForCategory(cat);
            var price = basePrice + (group * priceStep) + rng.Next(0, priceStep / 2);
            var rent = (int)Math.Max(1, Math.Round(price * 0.12));

            var b = Blocks[i];
            b.Type = BlockType.Property; b.Color = colorPalette[group % colorPalette.Length];
            b.ImageUrl = GetBuildingImage(cat, level);
            b.BuildingType = cat; b.BuildingPricesCsv = string.Join(',', prices);
            b.Name = BuildingEvolutionDescriptions.Get(cat, level).Name;
            b.Price = price; b.Rent = rent;

            idxInGroup++;
            if (idxInGroup >= 3) { idxInGroup = 0; group = (group + 1) % groups; }
        }

        StateHasChanged();
    }
}
