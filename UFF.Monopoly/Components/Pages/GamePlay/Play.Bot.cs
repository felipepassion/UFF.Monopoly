using Microsoft.AspNetCore.Components;
using UFF.Monopoly.Entities;
using UFF.Monopoly.Components.Pages.BoardBuilders;

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase, IAsyncDisposable
{
    private static bool IsBotPlayer(Player? p) => p?.Name?.StartsWith("Bot ", StringComparison.OrdinalIgnoreCase) == true;
    private bool IsCurrentPlayerHuman()
        => _game is not null && !_game.IsFinished && _game.CurrentPlayerIndex >= 0 && _game.CurrentPlayerIndex < _game.Players.Count && !IsBotPlayer(_game.Players[_game.CurrentPlayerIndex]);
    private bool IsCurrentPlayerBot()
        => _game is not null && !_game.IsFinished && _game.CurrentPlayerIndex >= 0 && _game.CurrentPlayerIndex < _game.Players.Count && IsBotPlayer(_game.Players[_game.CurrentPlayerIndex]);

    private async Task TryAutoRollForBotAsync()
    {
        if (_game is null) return;
        if (_isTypingChat) { if (IsCurrentPlayerBot()) _pendingBotRoll = true; return; }
        if (IsCurrentPlayerBot() && !_isAnimating && !HasRolledThisTurn)
        {
            try { await Task.Delay(1200); } catch { }
            if (!_isAnimating && !_isTypingChat)
            {
                await RollAndMove();
                if (!_showBlockModal)
                {
                    _game.NextTurn(); HasRolledThisTurn = false;
                    EnqueueGroup("transicao_turno", new DialogueContext { Player = _game.Players[_game.CurrentPlayerIndex].Name }, true);
                    await GameRepo.SaveGameAsync(GameId, _game);
                    AnnounceHumanTurnIfNeeded();
                    await TryAutoRollForBotAsync();
                }
            }
        }
        else
        { AnnounceHumanTurnIfNeeded(); }
    }

    private void TriggerBotModalIfNeeded(Player currentPlayer)
    { if (IsBotPlayer(currentPlayer)) { _botHasActedThisModal = false; _ = InvokeAsync(async () => { try { await Task.Delay(650); } catch { } await BotActOnModalAsync(); }); } }

    private async Task BotActOnModalAsync()
    { if (_game is null || _modalPlayer is null || !_modalFromMove) return; var idx = GetPlayerIndex(_modalPlayer.Id); if (!IsBotPlayer(_modalPlayer) || _botHasActedThisModal) return; _botHasActedThisModal = true; EnqueueGroup("bot_pensando", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock?.Name }, true); AdvanceDialogueIfIdle(); try { await Task.Delay(1400); } catch { } await TryBotPurchaseAsync(); try { await Task.Delay(1100); } catch { } await TryBotUpgradeAsync(); try { await Task.Delay(1400); } catch { } await CloseBlockModal(); }

    private async Task BotAutoActionsIfNeeded()
    { if (_game is null || _modalPlayer is null) return; if (!IsBotPlayer(_modalPlayer) || _botHasActedThisModal) return; EnqueueGroup("bot_pensando", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock?.Name }, true); AdvanceDialogueIfIdle(); try { await Task.Delay(1200); } catch { } await TryBotPurchaseAsync(); try { await Task.Delay(1000); } catch { } await TryBotUpgradeAsync(); }

    private async Task TryBotPurchaseAsync()
    { if (_modalBlock is not null && _modalBlock.Owner is null && (_modalBlock.Type == BlockType.Property || _modalBlock.Type == BlockType.Company) && _modalPlayer is not null && _game is not null && _modalPlayer.Money >= _modalBlock.Price) { EnqueueGroup("bot_acao_compra", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock.Name }, true, immediate: true); try { await Task.Delay(1200); } catch { } _game.TryBuyProperty(_modalPlayer, _modalBlock); await GameRepo.SaveGameAsync(GameId, _game); SyncOwnersToBoardSpaces(); StateHasChanged(); EnqueueGroup("acao_compra", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock.Name }, true, immediate: true); } }

    private async Task TryBotUpgradeAsync()
    { if (_modalBlock is PropertyBlock pb && _modalPlayer is not null && pb.Owner == _modalPlayer && CanUpgradeAllowed(pb)) { EnqueueGroup("bot_acao_upgrade", new DialogueContext { Player = _modalPlayer.Name, Block = pb.Name }, true, immediate: true); try { await Task.Delay(1150); } catch { } if (pb.Upgrade(_modalPlayer)) { if (pb.BuildingType != BuildingType.None && pb.BuildingLevel > 0) { var evo = BuildingEvolutionDescriptions.Get(pb.BuildingType, Math.Clamp(pb.BuildingLevel,1,4)); pb.Name = evo.Name; } _modalPlayer.LastBuildTurn = _game!.RoundCount; await GameRepo.SaveGameAsync(GameId, _game); SyncOwnersToBoardSpaces(); StateHasChanged(); EnqueueGroup("acao_upgrade", new DialogueContext { Player = _modalPlayer.Name, Block = pb.Name, Amount = pb.BuildingLevel }, true, immediate: true); } } }
}
