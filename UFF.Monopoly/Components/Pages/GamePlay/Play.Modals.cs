using Microsoft.AspNetCore.Components;
using UFF.Monopoly.Entities;
using UFF.Monopoly.Components.Pages.BoardBuilders;

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase, IAsyncDisposable
{
    private bool _showBlockModal; private Block? _modalBlock; private Player? _modalPlayer; private bool _modalFromMove; private Data.Entities.BlockTemplateEntity? _modalTemplateEntity; private int _preMovePlayerMoney;
    private PendingActionKind _pendingActionKind = PendingActionKind.None; private int _pendingAmount; private int _pendingBackSteps;
    private bool _showWinnerModal; private string _winnerName = string.Empty; private bool _showLoserModal; private string _loserName = string.Empty;
    private bool _showBotActionToast; private string _botActionMessage = string.Empty; private bool _botHasActedThisModal; private bool _everyoneDefeated;

    private void PrepareModalForLanding(Player currentPlayer)
    { if (_game is null) return; var landed = _game.Board.FirstOrDefault(b => b.Position == currentPlayer.CurrentPosition); ResetPendingSpecial(); if (landed is not null) { if (landed.Type == BlockType.Tax) ConfigureTax(landed, currentPlayer); else if (landed.Type == BlockType.Chance) ConfigureChance(landed); else if (landed.Type == BlockType.Reves) ConfigureReves(); } _modalPlayer = currentPlayer; _modalBlock = landed; _modalTemplateEntity = _templatesByPosition.TryGetValue(currentPlayer.CurrentPosition, out var tpl) ? tpl : null; _pawnAnimPosition = -1; _modalFromMove = true; _showBlockModal = true; }

    private async Task CloseBlockModal()
    { _showBlockModal = false; if (_modalFromMove && _game is not null && _modalPlayer is not null) { await ApplyPendingActionAsync(); if (_modalPlayer.Money <= 0) { await RegisterLoserAsync(_modalPlayer); CleanupModal(); ResetPendingSpecial(); return; } await BotAutoActionsIfNeeded(); _game.NextTurn(); HasRolledThisTurn = false; EnqueueGroup("transicao_turno", new DialogueContext { Player = _game.Players[_game.CurrentPlayerIndex].Name }, true); await GameRepo.SaveGameAsync(GameId, _game); } CleanupModal(); ResetPendingSpecial(); StateHasChanged(); AnnounceHumanTurnIfNeeded(); AdvanceDialogueIfIdle(); await TryAutoRollForBotAsync(); }

    private void CleanupModal() { _modalFromMove = false; _modalTemplateEntity = null; }
    private void ResetPendingSpecial() { _pendingActionKind = PendingActionKind.None; _pendingAmount = 0; _pendingBackSteps = 0; }

    private void ConfigureTax(Block landed, Player player) { _pendingActionKind = PendingActionKind.Tax; var val = landed.Rent > 0 ? landed.Rent : 150; _pendingAmount = val; landed.Rent = val; }
    // Ajuste: usar valor configurado (Rent) para Sorte, evitando sobrescrever com lista aleatória.
    private void ConfigureChance(Block landed) { _pendingActionKind = PendingActionKind.Chance; var val = landed.Rent != 0 ? landed.Rent : 2; _pendingAmount = val; landed.Rent = val; }
    private void ConfigureReves() { var takeMoneyOptions = new[] { 100, 200 }; _pendingActionKind = PendingActionKind.Reves; if (_rand.NextDouble() < 0.5) { _pendingAmount = takeMoneyOptions[_rand.Next(takeMoneyOptions.Length)]; } else { _pendingBackSteps = _rand.Next(2, 7); } }

    private async Task ApplyPendingActionAsync() { if (_game is null || _modalPlayer is null || _pendingActionKind == PendingActionKind.None) return; var player = _modalPlayer; var landed = _game.Board.FirstOrDefault(b => b.Position == player.CurrentPosition); switch (_pendingActionKind) { case PendingActionKind.Tax: player.Money = Math.Max(0, player.Money - _pendingAmount); if (landed is not null) landed.Rent = _pendingAmount; EnqueueGroup("evento_tax", new DialogueContext { Player = player.Name, Amount = _pendingAmount }, true, immediate: true); break; case PendingActionKind.Chance: player.Money += _pendingAmount; if (landed is not null) landed.Rent = _pendingAmount; EnqueueGroup("evento_chance", new DialogueContext { Player = player.Name, Amount = _pendingAmount }, true, immediate: true); break; case PendingActionKind.Reves: if (_pendingBackSteps > 0) { await AnimateBackwardAsync(GetPlayerIndex(player.Id), _pendingBackSteps); EnqueueGroup("evento_reves", new DialogueContext { Player = player.Name, Steps = _pendingBackSteps }, true, immediate: true); } else if (_pendingAmount > 0) { player.Money = Math.Max(0, player.Money - _pendingAmount); EnqueueGroup("evento_reves", new DialogueContext { Player = player.Name, Amount = _pendingAmount }, true, immediate: true); } break; } await GameRepo.SaveGameAsync(GameId, _game); ResetPendingSpecial(); AdvanceDialogueIfIdle(); }

    private async Task OnBuyPropertyAsync() { if (_modalBlock is null || _modalPlayer is null) return; if (_game!.TryBuyProperty(_modalPlayer, _modalBlock)) { await GameRepo.SaveGameAsync(GameId, _game); EnqueueGroup("acao_compra", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock.Name }, true, immediate: true); SyncOwnersToBoardSpaces(); await ShowActionToastAsync($"{_modalPlayer.Name} comprou {_modalBlock.Name}"); } StateHasChanged(); }
    private async Task OnUpgradeAsync() { if (_modalBlock is PropertyBlock pb && _modalPlayer is not null && CanUpgradeAllowed(pb)) { if (pb.Upgrade(_modalPlayer)) { if (pb.BuildingType != BuildingType.None && pb.BuildingLevel > 0) { var evo = BuildingEvolutionDescriptions.Get(pb.BuildingType, Math.Clamp(pb.BuildingLevel,1,4)); pb.Name = evo.Name; } await GameRepo.SaveGameAsync(GameId, _game); SyncOwnersToBoardSpaces(); StateHasChanged(); EnqueueGroup("acao_upgrade", new DialogueContext { Player = _modalPlayer.Name, Block = pb.Name, Amount = pb.BuildingLevel }, true, immediate: true); } StateHasChanged(); } }
    private async Task OnSellPropertyAsync() { if (_modalBlock is PropertyBlock pb && pb.Owner is not null) { var owner = pb.Owner; owner.Money += pb.Price / 2; owner.OwnedProperties.Remove(pb); pb.Owner = null; pb.IsMortgaged = false; await GameRepo.SaveGameAsync(GameId, _game!); EnqueueGroup("acao_venda", new DialogueContext { Player = owner.Name, Block = pb.Name }, true, immediate: true); await ShowActionToastAsync($"{owner.Name} vendeu {pb.Name}"); StateHasChanged(); } }
    private bool CanUpgradeAllowed(PropertyBlock pb) { if (_game is null || _modalPlayer is null) return false; if (pb.Owner != _modalPlayer) return false; if (_modalPlayer.CurrentPosition != pb.Position) return false; if (pb.BuildingType == BuildingType.None) return false; if (!pb.CanUpgrade()) return false; var nextCost = pb.BuildingPrices[pb.BuildingLevel]; if (_modalPlayer.Money < nextCost) return false; return true; }
    private int GetNextUpgradeCost(PropertyBlock pb) => pb.CanUpgrade() ? pb.BuildingPrices[pb.BuildingLevel] : 0;

    private async Task RegisterLoserAsync(Player player)
    { if (_game is null) return; player.IsBankrupt = true; foreach (var prop in player.OwnedProperties) { prop.Owner = null; prop.IsMortgaged = false; } player.OwnedProperties.Clear(); _loserName = player.Name; var ativos = _game.Players.Count(p => !p.IsBankrupt); if (ativos == 1) { var last = _game.Players.First(p => !p.IsBankrupt); var anyHumanAlive = _game.Players.Any(p => !p.IsBankrupt && !IsBotPlayer(p)); if (!anyHumanAlive && IsBotPlayer(last)) { _everyoneDefeated = true; _showWinnerModal = false; _showLoserModal = true; _winnerName = string.Empty; _game.Finish(); EnqueueGroup("eliminacao", new DialogueContext { Player = player.Name }, true); EnqueueGroup("fim_todos_derrotados", new DialogueContext { Player = last.Name }, true); } else { _winnerName = last.Name; _showWinnerModal = true; _showLoserModal = false; _everyoneDefeated = false; _game.Finish(); EnqueueGroup("vitoria", new DialogueContext { Player = last.Name }, true); } } else { _showLoserModal = true; _showWinnerModal = false; _everyoneDefeated = false; _game.NextTurn(); HasRolledThisTurn = false; EnqueueGroup("eliminacao", new DialogueContext { Player = player.Name }, true); } await GameRepo.SaveGameAsync(GameId, _game); StateHasChanged(); AdvanceDialogueIfIdle(); }

    private void CloseLoserModal() { _showLoserModal = false; StateHasChanged(); _ = TryAutoRollForBotAsync(); }
    private void CloseWinnerModal() { _showWinnerModal = false; StateHasChanged(); }

    private async Task RestartGameAsync() { if (boardId.HasValue) Navigation.NavigateTo($"/local?boardId={boardId.Value}"); else Navigation.NavigateTo("/local"); await Task.CompletedTask; }
    private Task BackToMenuAsync() { Navigation.NavigateTo("/"); return Task.CompletedTask; }

    // Animated toast helper: shows message, holds, then hides with CSS animation
    private async Task ShowActionToastAsync(string message, int durationMs = 2000)
    {
        _botActionMessage = message;
        _showBotActionToast = true;
        StateHasChanged();
        try { await Task.Delay(durationMs); } catch { }
        _showBotActionToast = false;
        StateHasChanged();
    }
}
