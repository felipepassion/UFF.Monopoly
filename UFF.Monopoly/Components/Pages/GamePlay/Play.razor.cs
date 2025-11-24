using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using UFF.Monopoly.Data;
using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Entities;
using UFF.Monopoly.Models;
using UFF.Monopoly.Repositories;

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase
{
    [Inject] public IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;
    [Inject] public IGameRepository GameRepo { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public Infrastructure.IUserProfileService Profiles { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public Guid GameId { get; set; }
    [SupplyParameterFromQuery] public Guid? boardId { get; set; }
    [SupplyParameterFromQuery(Name = "humanCount")] public int? HumanCountQuery { get; set; }
    [SupplyParameterFromQuery] public string? pawns { get; set; }

    private List<BoardSpaceDto> BoardSpaces { get; set; } = new();
    private Dictionary<int, BlockTemplateEntity> _templatesByPosition = new();
    private Game? _game; private bool _loading = true;
    private int Rows; private int Cols; private int CellSize; private List<(int r, int c)> Perimeter = new();
    private string PawnUrl = "/images/pawns/PawnsB1.png";
    private int _pawnAnimPosition = -1; private bool _isAnimating; private int _animStepMs = 140;
    private bool _showDiceOverlay; private int _diceFace1 = 1; private int _diceFace2 = 1; private string _rollingGifUrl = string.Empty; private readonly Random _rand = new();
    private const double _boardScale = 1.5; private string _boardWidthCss = "0px"; private string _boardHeightCss = "0px";
    private List<int> _pawnsForPlayers = new();
    private bool _showBlockModal; private Block? _modalBlock; private Player? _modalPlayer; private int _preMovePlayerMoney; private bool _modalFromMove;
    private BlockTemplateEntity? _modalTemplateEntity;
    private PendingActionKind _pendingActionKind = PendingActionKind.None; private int _pendingAmount = 0; private int _pendingBackSteps = 0;
    private bool _showWinnerModal; private string _winnerName = string.Empty; private bool _showLoserModal; private string _loserName = string.Empty;
    private bool _showBotActionToast; private string _botActionMessage = string.Empty; private bool _botHasActedThisModal = false;

    protected override async Task OnParametersSetAsync() => await InitializeAsync();

    private async Task InitializeAsync()
    {
        _loading = true;
        if (!boardId.HasValue) { Navigation.NavigateTo("/local"); return; }
        await LoadBoardLayoutAsync(boardId.Value);
        _game = await GameRepo.GetGameAsync(GameId);
        if (_game is not null)
        {
            _pawnAnimPosition = _game.Players.FirstOrDefault()?.CurrentPosition ?? 0;
            _pawnsForPlayers = _game.Players.Select(p => Math.Clamp(p.PawnIndex, 1, 6)).ToList();
        }
        if (!_pawnsForPlayers.Any()) ParsePawnsQuery();
        try { var pawn = await Profiles.GetPawnFromSessionAsync(); if (!string.IsNullOrWhiteSpace(pawn)) PawnUrl = pawn; } catch { }
        _loading = false; StateHasChanged();
        await TryAutoRollForBotAsync();
    }

    private void ParsePawnsQuery()
    { _pawnsForPlayers.Clear(); if (string.IsNullOrWhiteSpace(pawns)) return; var parts = pawns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); foreach (var p in parts) { if (int.TryParse(p, out var v)) _pawnsForPlayers.Add(Math.Clamp(v, 1, 6)); } }
    private string GetPawnUrlForPlayer(int playerIndex) => playerIndex < _pawnsForPlayers.Count ? $"{Navigation.BaseUri}images/pawns/PawnsB{_pawnsForPlayers[playerIndex]}.png" : PawnUrl;

    private async Task LoadBoardLayoutAsync(Guid bId)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var board = await db.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bId); if (board is null) return;
        Rows = board.Rows; Cols = board.Cols; CellSize = board.CellSizePx; Perimeter = BuildPerimeterClockwise(Rows, Cols);
        var templates = await db.BlockTemplates.AsNoTracking().Where(t => t.BoardDefinitionId == bId).OrderBy(t => t.Position).ToListAsync();
        _templatesByPosition = templates.ToDictionary(t => t.Position, t => t);
        BoardSpaces = new List<BoardSpaceDto>(Perimeter.Count);
        for (int i = 0; i < Perimeter.Count; i++)
        {
            var (r,c) = Perimeter[i];
            var template = templates.ElementAtOrDefault(i);
            var img = template != null && !string.IsNullOrWhiteSpace(template.ImageUrl) ? template.ImageUrl : GetImageForType(template?.Type ?? BlockType.Property);
            BoardSpaces.Add(new BoardSpaceDto {
                Id = $"space-{i}",
                Name = template?.Name ?? $"Space {i}",
                ImageUrl = img,
                Price = template?.Price ?? 0,
                Rent = template?.Rent ?? 0,
                Type = template?.Type ?? BlockType.Property,
                BuildingType = template?.BuildingType ?? BuildingType.None,
                BuildingLevel = (template != null && template.Level.HasValue) ? (int)template.Level.Value : 0,
                Style = new BoardSpaceStyle {
                    Top = $"{(int)(r*CellSize*_boardScale)}px",
                    Left = $"{(int)(c*CellSize*_boardScale)}px",
                    Width = $"{(int)(CellSize*_boardScale)}px",
                    Height = $"{(int)(CellSize*_boardScale)}px"
                }
            });
        }
        _boardWidthCss = ((int)(Cols*CellSize*_boardScale)) + "px"; _boardHeightCss = ((int)(Rows*CellSize*_boardScale)) + "px";
    }

    private async Task RollForCurrentPlayer() { if (_game is null || _isAnimating) return; if (_game.CurrentPlayerIndex >= GetHumanPlayersCount()) return; await RollAndMove(); }

    private async Task RollAndMove()
    {
        if (_game is null || _isAnimating) return;
        var currentPlayer = _game.Players[_game.CurrentPlayerIndex];
        if (currentPlayer.Money < 0) { await RegisterLoserAsync(currentPlayer); return; }
        _isAnimating = true;
        var (die1, die2, total) = _game.RollDice();
        await ShowDiceAnimationAsync(die1, die2);
        await AnimateForwardAsync(total);
        _preMovePlayerMoney = currentPlayer.Money;
        await _game.MoveCurrentPlayerAsync(total);
        await GameRepo.SaveGameAsync(GameId, _game);
        PrepareModalForLanding(currentPlayer);
        _isAnimating = false;
        StateHasChanged();
        TriggerBotModalIfNeeded(currentPlayer);
    }

    private async Task AnimateForwardAsync(int steps)
    {
        if (_game is null) return;
        var currentPlayer = _game.Players[_game.CurrentPlayerIndex];
        var pos = currentPlayer.CurrentPosition;
        for (int i = 0; i < steps; i++)
        {
            pos = (pos + 1) % Perimeter.Count;
            _pawnAnimPosition = pos;
            StateHasChanged();
            try { await Task.Delay(_animStepMs); } catch { }
        }
    }

    private void PrepareModalForLanding(Player currentPlayer)
    {
        if (_game is null) return;
        var landed = _game.Board.FirstOrDefault(b => b.Position == currentPlayer.CurrentPosition);
        _pendingActionKind = PendingActionKind.None; _pendingAmount = 0; _pendingBackSteps = 0;
        if (landed is not null)
        {
            if (landed.Type == BlockType.Tax) ConfigureTax(landed, currentPlayer);
            else if (landed.Type == BlockType.Chance) ConfigureChance();
            else if (landed.Type == BlockType.Reves) ConfigureReves();
        }
        _modalPlayer = currentPlayer; _modalBlock = landed; _modalTemplateEntity = _templatesByPosition.TryGetValue(currentPlayer.CurrentPosition, out var tpl) ? tpl : null;
        _pawnAnimPosition = -1; _modalFromMove = true; _showBlockModal = true;
    }

    private void ConfigureTax(Block landed, Player player)
    { var percents = new[] { 5, 10, 15, 20, 25, 30 }; var pct = percents[_rand.Next(percents.Length)]; var taxAmount = (int)Math.Round(player.Money * pct / 100.0); _pendingActionKind = PendingActionKind.Tax; _pendingAmount = taxAmount; landed.Rent = taxAmount; }
    private void ConfigureChance() { var choices = new[] { 50, 100, 150, 200, 300, 350, 400, 500, 600, 700 }; _pendingActionKind = PendingActionKind.Chance; _pendingAmount = choices[_rand.Next(choices.Length)]; }
    private void ConfigureReves() { var takeMoneyOptions = new[] { 100, 200 }; _pendingActionKind = PendingActionKind.Reves; if (_rand.NextDouble() < 0.5) { _pendingAmount = takeMoneyOptions[_rand.Next(takeMoneyOptions.Length)]; } else { _pendingBackSteps = _rand.Next(2,7); } }

    private void TriggerBotModalIfNeeded(Player currentPlayer)
    { var humanCount = GetHumanPlayersCount(); if (GetPlayerIndex(currentPlayer.Id) >= humanCount) { _botHasActedThisModal = false; _ = InvokeAsync(async () => { try { await Task.Delay(150); } catch { } await BotActOnModalAsync(); }); } }

    private async Task ApplyPendingActionAsync()
    {
        if (_game is null || _modalPlayer is null || _pendingActionKind == PendingActionKind.None) return; var player = _modalPlayer; var landed = _game.Board.FirstOrDefault(b => b.Position == player.CurrentPosition);
        switch (_pendingActionKind)
        {   case PendingActionKind.Tax: player.Money = Math.Max(0, player.Money - _pendingAmount); if (landed is not null) landed.Rent = _pendingAmount; break;
            case PendingActionKind.Chance: player.Money += _pendingAmount; break;
            case PendingActionKind.Reves: if (_pendingBackSteps > 0) await AnimateBackwardAsync(GetPlayerIndex(player.Id), _pendingBackSteps); else if (_pendingAmount > 0) player.Money = Math.Max(0, player.Money - _pendingAmount); break; }
        await GameRepo.SaveGameAsync(GameId, _game); _pendingActionKind = PendingActionKind.None; _pendingAmount = 0; _pendingBackSteps = 0;
    }

    private async Task CloseBlockModal()
    {
        _showBlockModal = false;
        if (_modalFromMove && _game is not null && _modalPlayer is not null)
        {
            await ApplyPendingActionAsync();
            if (_modalPlayer.Money < 0) { await RegisterLoserAsync(_modalPlayer); CleanupModal(); return; }
            await BotAutoActionsIfNeeded();
            _game.NextTurn(); await GameRepo.SaveGameAsync(GameId, _game);
        }
        CleanupModal(); StateHasChanged(); await TryAutoRollForBotAsync();
    }

    private async Task BotAutoActionsIfNeeded()
    { var humanCount = GetHumanPlayersCount(); var actorIndex = GetPlayerIndex(_modalPlayer!.Id); if (actorIndex < humanCount || _botHasActedThisModal) return; await TryBotPurchaseAsync(); await TryBotUpgradeAsync(); try { await Task.Delay(400); } catch { } }

    private async Task TryBotPurchaseAsync()
    { if (_modalBlock is not null && _modalBlock.Owner is null && (_modalBlock.Type == BlockType.Property || _modalBlock.Type == BlockType.Company) && _modalPlayer!.Money >= _modalBlock.Price) { _game!.TryBuyProperty(_modalPlayer, _modalBlock); await GameRepo.SaveGameAsync(GameId, _game); await ShowBotActionToast($"{_modalPlayer.Name} comprou {_modalBlock.Name}"); } }
    private async Task TryBotUpgradeAsync()
    { if (_modalBlock is PropertyBlock pb && pb.Owner == _modalPlayer && CanUpgradeAllowed(pb)) { var ok = pb.Upgrade(_modalPlayer!); if (ok) { _modalPlayer!.LastBuildTurn = _game!.RoundCount; await GameRepo.SaveGameAsync(GameId, _game); await ShowBotActionToast($"{_modalPlayer.Name} evoluiu {pb.Name} para nível {pb.BuildingLevel}"); } } }

    private void CleanupModal() { _modalFromMove = false; _modalTemplateEntity = null; }

    private async Task RegisterLoserAsync(Player player)
    { if (_game is null) return; player.IsBankrupt = true; foreach (var prop in player.OwnedProperties) { prop.Owner = null; prop.IsMortgaged = false; } player.OwnedProperties.Clear(); _loserName = player.Name; var ativos = _game.Players.Count(p => !p.IsBankrupt); if (ativos == 1) { var winner = _game.Players.First(p => !p.IsBankrupt); _winnerName = winner.Name; _showWinnerModal = true; _showLoserModal = false; _game.Finish(); } else { _showLoserModal = true; _showWinnerModal = false; _game.NextTurn(); } await GameRepo.SaveGameAsync(GameId, _game); StateHasChanged(); }

    private async Task BotActOnModalAsync()
    { if (_game is null || _modalPlayer is null || !_modalFromMove) return; var humanCount = GetHumanPlayersCount(); var actorIndex = GetPlayerIndex(_modalPlayer.Id); if (actorIndex < humanCount || _botHasActedThisModal) return; _botHasActedThisModal = true; try { await Task.Delay(500); } catch { } await TryBotPurchaseAsync(); await TryBotUpgradeAsync(); try { await Task.Delay(1200); } catch { } await CloseBlockModal(); }

    private async Task ShowDiceAnimationAsync(int finalDie1, int finalDie2)
    { try { var gifIndex = _rand.Next(1, 13); _rollingGifUrl = $"{Navigation.BaseUri}images/diceAnim/dice-rolling-{gifIndex}.gif"; _showDiceOverlay = true; StateHasChanged(); var animDurationMs = 800; var frameMs = 80; var frames = Math.Max(1, animDurationMs / frameMs); for (int i = 0; i < frames; i++) { _diceFace1 = _rand.Next(1,7); _diceFace2 = _rand.Next(1,7); StateHasChanged(); try { await Task.Delay(frameMs); } catch { } } _diceFace1 = Math.Clamp(finalDie1,1,6); _diceFace2 = Math.Clamp(finalDie2,1,6); StateHasChanged(); } finally { _showDiceOverlay = false; _rollingGifUrl = string.Empty; StateHasChanged(); } }

    private async Task AnimateBackwardAsync(int playerIndex, int steps)
    { if (_game is null || playerIndex < 0 || playerIndex >= _game.Players.Count) return; var prev = _isAnimating; _isAnimating = true; var player = _game.Players[playerIndex]; var pos = player.CurrentPosition; for (int i = 0; i < steps; i++) { pos = (pos - 1 + Perimeter.Count) % Perimeter.Count; _pawnAnimPosition = pos; StateHasChanged(); try { await Task.Delay(_animStepMs); } catch { } } player.CurrentPosition = pos; _pawnAnimPosition = -1; _isAnimating = prev; }

    private int GetHumanPlayersCount()
    { if (HumanCountQuery.HasValue) return Math.Max(0, HumanCountQuery.Value); try { var uri = Navigation.ToAbsoluteUri(Navigation.Uri); var q = QueryHelpers.ParseQuery(uri.Query); if (q.TryGetValue("humanCount", out var hv) && int.TryParse(hv.ToString(), out var parsed)) return Math.Max(0, parsed); } catch { } return 1; }

    private bool CanRollForCurrent => !_isAnimating && _game is not null && (_game.CurrentPlayerIndex < GetHumanPlayersCount());
    private Task EndGame() { Navigation.NavigateTo("/"); return Task.CompletedTask; }

    private int GetPlayerIndex(Guid playerId)
    { if (_game is null) return -1; for (int i = 0; i < _game.Players.Count; i++) if (_game.Players[i].Id == playerId) return i; return -1; }

    private async Task ShowBotActionToast(string message)
    { _botActionMessage = message; _showBotActionToast = true; StateHasChanged(); try { await Task.Delay(1200); } catch { } _showBotActionToast = false; StateHasChanged(); }

    private bool CanUpgradeAllowed(PropertyBlock pb)
    { if (_game is null || _modalPlayer is null) return false; if (pb.Owner != _modalPlayer) return false; if (_modalPlayer.CurrentPosition != pb.Position) return false; if (_modalPlayer.LastPurchaseTurn >= 0 && _game.RoundCount <= _modalPlayer.LastPurchaseTurn) return false; if (_modalPlayer.LastBuildTurn == _game.RoundCount) return false; if (pb.BuildingType == BuildingType.None) return false; if (!pb.CanUpgrade()) return false; var nextCost = pb.BuildingPrices[pb.BuildingLevel]; if (_modalPlayer.Money < nextCost) return false; return true; }
    private int GetNextUpgradeCost(PropertyBlock pb) => pb.CanUpgrade() ? pb.BuildingPrices[pb.BuildingLevel] : 0;

    private async Task TryAutoRollForBotAsync()
    { if (_game is null) return; var human = GetHumanPlayersCount(); if (_game.CurrentPlayerIndex >= human && !_isAnimating) { try { await Task.Delay(700); } catch { } if (!_isAnimating) await RollAndMove(); } }

    private static List<(int r, int c)> BuildPerimeterClockwise(int rows, int cols)
    { var list = new List<(int r, int c)>(Math.Max(0, 2*rows + 2*cols - 4)); if (rows < 2 || cols < 2) return list; int bottom = rows - 1; int top = 0; int left = 0; int right = cols - 1; for (int c = right; c >= left; c--) list.Add((bottom,c)); for (int r = bottom - 1; r >= top; r--) list.Add((r,left)); for (int c = left + 1; c <= right; c++) list.Add((top,c)); for (int r = top + 1; r <= bottom - 1; r++) list.Add((r,right)); return list; }

    private static string GetImageForType(BlockType? type) => type switch
    { BlockType.Go => "/images/blocks/property_basic.svg", BlockType.Property => "/images/blocks/property_basic.svg", BlockType.Company => "/images/blocks/property_predio.svg", BlockType.Jail => "/images/blocks/visitar_prisao.svg", BlockType.GoToJail => "/images/blocks/go_to_jail.svg", BlockType.Tax => "/images/blocks/volte-casas.svg", _ => "/images/blocks/property_basic.svg" };

    private async Task HandleClick(BoardSpaceDto space)
    { if (_game is null || space is null) return; var parts = (space.Id ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); if (parts.Length < 2) return; if (!int.TryParse(parts[1], out var pos)) return; var block = _game.Board.FirstOrDefault(b => b.Position == pos); if (block is null) return; _modalFromMove = false; _modalBlock = block; _modalPlayer = _game.Players.ElementAtOrDefault(_game.CurrentPlayerIndex); _preMovePlayerMoney = _modalPlayer?.Money ?? 0; _modalTemplateEntity = _templatesByPosition.TryGetValue(pos, out var tmpl) ? tmpl : null; _showBlockModal = true; StateHasChanged(); }

    private async Task OnBuyPropertyAsync()
    { if (_modalBlock is null || _modalPlayer is null) return; var success = _game!.TryBuyProperty(_modalPlayer, _modalBlock); if (success) await GameRepo.SaveGameAsync(GameId, _game); StateHasChanged(); }

    private async Task OnUpgradeAsync()
    { if (_modalBlock is PropertyBlock pb && _modalPlayer is not null) { if (!CanUpgradeAllowed(pb)) return; var ok = pb.Upgrade(_modalPlayer); if (ok) { _modalPlayer.LastBuildTurn = _game?.RoundCount ?? 0; if (_game is not null) await GameRepo.SaveGameAsync(GameId, _game); } StateHasChanged(); } }

    private async Task OnSellPropertyAsync()
    { if (_modalBlock is PropertyBlock pb && pb.Owner is not null) { var owner = pb.Owner; owner.Money += pb.Price / 2; owner.OwnedProperties.Remove(pb); pb.Owner = null; pb.IsMortgaged = false; if (_game is not null) await GameRepo.SaveGameAsync(GameId, _game); StateHasChanged(); } }

    private void CloseLoserModal() { _showLoserModal = false; StateHasChanged(); _ = TryAutoRollForBotAsync(); }
    private void CloseWinnerModal() { _showWinnerModal = false; StateHasChanged(); }
}
