using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
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
    internal bool ShowToast; internal string ToastMessage = string.Empty;

    protected override async Task OnInitializedAsync() => await LoadBoardsListAsync();

    protected override async Task OnParametersSetAsync()
    { if (BoardId.HasValue && BoardId.Value != Guid.Empty && BoardId.Value != CurrentBoardId) { await LoadBoardAsync(BoardId.Value); } if (Saved && !_savedToastShown) { _savedToastShown = true; _ = ShowSuccessToast("Salvo com sucesso!"); } }

    private async Task LoadBoardsListAsync()
    { await using var db = await DbFactory.CreateDbContextAsync(); ExistingBoards = await db.Boards.AsNoTracking().OrderByDescending(b => b.CreatedAt).ToListAsync(); }

    private async Task LoadBoardAsync(Guid id)
    { await using var db = await DbFactory.CreateDbContextAsync(); var board = await db.Boards.Include(b => b.Blocks).FirstOrDefaultAsync(b => b.Id == id); if (board is null) return; CurrentBoardId = id; Form.Name = board.Name; Form.Rows = board.Rows; Form.Cols = board.Cols; Form.CellSize = board.CellSizePx; Blocks = board.Blocks.OrderBy(b => b.Position).ToList(); RebuildPerimeterMap(); StateHasChanged(); }

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
    { var idx = Popup.Index; if (idx < 0 || idx >= Blocks.Count) { Popup.Visible = false; return; } if (type == BlockType.Property || type == BlockType.Company) { CategoryModal = (true, idx); Popup.Visible = false; StateHasChanged(); return; } Blocks[idx].Type = type; Blocks[idx].Color = GetTypeColor(type); Blocks[idx].ImageUrl = GetDefaultImageForType(type); Blocks[idx].Name = TranslateBlockType(type); Popup.Visible = false; StateHasChanged(); }

    internal void CloseCategoryModal() { CategoryModal = (false, -1); StateHasChanged(); }

    internal void ApplyCategory(BuildingType cat)
    { var idx = CategoryModal.Index; if (idx < 0 || idx >= Blocks.Count) { CloseCategoryModal(); return; } Blocks[idx].Type = (cat == BuildingType.Company) ? BlockType.Company : BlockType.Property; Blocks[idx].BuildingType = cat; Blocks[idx].ImageUrl = GetBuildingImage(cat, 1); var prices = GetDefaultPricesForCategory(cat); Blocks[idx].BuildingPricesCsv = string.Join(',', prices); Blocks[idx].Name = GetCategoryName(cat); CloseCategoryModal(); StateHasChanged(); }

    internal void SelectCategoryShortcut(BuildingType cat)
    { var idx = Popup.Index; if (idx < 0 || idx >= Blocks.Count) { Popup.Visible = false; StateHasChanged(); return; } Blocks[idx].Type = (cat == BuildingType.Company) ? BlockType.Company : BlockType.Property; Blocks[idx].BuildingType = cat; Blocks[idx].ImageUrl = GetBuildingImage(cat, 1); var prices = GetDefaultPricesForCategory(cat); Blocks[idx].BuildingPricesCsv = string.Join(',', prices); Blocks[idx].Name = GetCategoryName(cat); Popup.Visible = false; StateHasChanged(); }

    internal string GetCellBackground(BlockTemplateEntity b) => string.IsNullOrWhiteSpace(b.Color) ? GetTypeColor(b.Type) : b.Color;

    internal string GetTypeColor(BlockType t) => t switch { BlockType.Go => "#27ae60", BlockType.Property => "#3498db", BlockType.Company => "#2ecc71", BlockType.Tax => "#e74c3c", BlockType.Jail => "#f1c40f", BlockType.GoToJail => "#d35400", BlockType.Chance => "#8e44ad", BlockType.Reves => "#f39c12", BlockType.FreeParking => "#16a085", _ => "#95a5a6" };

    internal string GetDefaultImageForType(BlockType t) => t switch { BlockType.Property => "/images/board/buildings/house1.png", BlockType.Company => "/images/board/buildings/company1.png", BlockType.GoToJail => "/images/blocks/go_to_jail.svg", BlockType.Jail => "/images/blocks/visitar_prisao.svg", BlockType.Tax => "/images/blocks/caminho_estrada_diagonal.svg", BlockType.Go => "/images/blocks/volte-casas.svg", BlockType.Chance => "/images/blocks/caminho_praia.svg", BlockType.Reves => "/images/blocks/caminho_reto_campo.svg", BlockType.FreeParking => "/images/board/buildings/special_circus.png", _ => "/images/board/buildings/house1.png" };

    internal string GetCategoryName(BuildingType cat) => cat switch { BuildingType.House => "Casas", BuildingType.Hotel => "Hotéis", BuildingType.Company => "Empresas", BuildingType.Special => "Prédios Especiais", _ => cat.ToString() };
    internal string GetCategoryColor(BuildingType cat) => cat switch { BuildingType.House => GetTypeColor(BlockType.Property), BuildingType.Hotel => GetTypeColor(BlockType.Property), BuildingType.Company => GetTypeColor(BlockType.Company), BuildingType.Special => GetTypeColor(BlockType.FreeParking), _ => "#95a5a6" };
    internal static int[] GetDefaultPricesForCategory(BuildingType cat) => cat switch { BuildingType.House => new[] { 100, 200, 350, 500 }, BuildingType.Hotel => new[] { 300, 600, 1000, 1500 }, BuildingType.Company => new[] { 400, 800, 1300, 1900 }, BuildingType.Special => new[] { 500, 1000, 1600, 2300 }, _ => new[] { 0, 0, 0, 0 } };
    internal static string GetBuildingImage(BuildingType cat, int level) => cat switch { BuildingType.House => $"/images/board/buildings/house{Math.Clamp(level,1,4)}.png", BuildingType.Hotel => $"/images/board/buildings/hotel{Math.Clamp(level,1,4)}.png", BuildingType.Company => $"/images/board/buildings/company{Math.Clamp(level,1,4)}.png", BuildingType.Special => Math.Clamp(level,1,4) switch { 1 => "/images/board/buildings/special_circus.png", 2 => "/images/board/buildings/special_shopping.png", 3 => "/images/board/buildings/special_stadium.png", _ => "/images/board/buildings/special_airport.png" }, _ => "/images/board/buildings/house1.png" };

    internal async Task SaveAsync()
    { await using var db = await DbFactory.CreateDbContextAsync(); var isNew = CurrentBoardId == Guid.Empty; if (isNew) { var newBoard = new BoardDefinitionEntity { Id = Guid.NewGuid(), Name = string.IsNullOrWhiteSpace(Form.Name) ? $"Board {DateTime.UtcNow:yyyyMMddHHmmss}" : Form.Name.Trim(), Rows = Form.Rows, Cols = Form.Cols, CellSizePx = Form.CellSize }; db.Boards.Add(newBoard); await db.SaveChangesAsync(); CurrentBoardId = newBoard.Id; } else { var boardOnly = new BoardDefinitionEntity { Id = CurrentBoardId }; db.Attach(boardOnly); db.Entry(boardOnly).Property(x => x.Name).CurrentValue = Form.Name.Trim(); db.Entry(boardOnly).Property(x => x.Rows).CurrentValue = Form.Rows; db.Entry(boardOnly).Property(x => x.Cols).CurrentValue = Form.Cols; db.Entry(boardOnly).Property(x => x.CellSizePx).CurrentValue = Form.CellSize; db.Entry(boardOnly).Property(x => x.Name).IsModified = true; db.Entry(boardOnly).Property(x => x.Rows).IsModified = true; db.Entry(boardOnly).Property(x => x.Cols).IsModified = true; db.Entry(boardOnly).Property(x => x.CellSizePx).IsModified = true; await db.SaveChangesAsync(); var existing = await db.BlockTemplates.Where(x => x.BoardDefinitionId == CurrentBoardId).ToListAsync(); if (existing.Count > 0) { db.BlockTemplates.RemoveRange(existing); await db.SaveChangesAsync(); } } var toInsert = new List<BlockTemplateEntity>(Blocks.Count); for (var i = 0; i < Blocks.Count; i++) { var b = Blocks[i]; toInsert.Add(new BlockTemplateEntity { Id = Guid.NewGuid(), Position = i, Name = b.Name, Description = b.Description, ImageUrl = b.ImageUrl, Color = b.Color, Price = b.Price, Rent = b.Rent, Type = b.Type, Level = b.Level, HousePrice = b.HousePrice, HotelPrice = b.HotelPrice, RentsCsv = b.RentsCsv, BuildingType = b.BuildingType, BuildingPricesCsv = b.BuildingPricesCsv, BoardDefinitionId = CurrentBoardId }); } db.BlockTemplates.AddRange(toInsert); await db.SaveChangesAsync(); await LoadBoardsListAsync(); if (isNew) { Nav.NavigateTo($"/board-builder/{CurrentBoardId}?saved=true"); } else { await ShowSuccessToast("Salvo com sucesso!"); } StateHasChanged(); }

    internal async Task ShowSuccessToast(string message) { ToastMessage = message; ShowToast = true; StateHasChanged(); try { await Task.Delay(2000); } catch { } ShowToast = false; StateHasChanged(); }

    internal string TranslateBlockType(BlockType t) => t switch { BlockType.Go => "Início", BlockType.Property => "Propriedade", BlockType.Company => "Companhia", BlockType.Tax => "Taxa", BlockType.Jail => "Visitar Prisão", BlockType.GoToJail => "Vá para Prisão", BlockType.Chance => "Sorte", BlockType.Reves => "Revés", BlockType.FreeParking => "Parada Livre", _ => t.ToString() };
}
