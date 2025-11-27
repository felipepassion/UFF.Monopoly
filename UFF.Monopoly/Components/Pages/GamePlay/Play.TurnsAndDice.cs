using Microsoft.AspNetCore.Components;

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase, IAsyncDisposable
{
    private bool _isAnimating; private int _animStepMs = 140; private bool _showDiceOverlay; private int _diceFace1 = 1; private int _diceFace2 = 1; private string _rollingGifUrl = string.Empty;
    private bool HasRolledThisTurn; private bool _pendingHumanRoll; private bool _pendingBotRoll;

    private bool IsPlayerTurn => IsCurrentPlayerHuman();
    private bool CanRollDice => IsPlayerTurn && !HasRolledThisTurn && !_isAnimating && !_showBlockModal && !_showWinnerModal && !_showLoserModal && !_isTypingChat;
    private bool IsDiceAnimating => _isAnimating || _showDiceOverlay;
    private bool CanEndTurn => IsPlayerTurn && HasRolledThisTurn && !_isAnimating && !_showBlockModal && !_showWinnerModal && !_showLoserModal;

    private async Task RollForCurrentPlayer()
    { if (_game is null || _isAnimating || HasRolledThisTurn) return; if (!IsCurrentPlayerHuman()) return; if (_isTypingChat) { _pendingHumanRoll = true; return; } await RollAndMove(); }

    private async Task RollAndMove()
    {
        if (_game is null || _isAnimating) return; var currentPlayer = _game.Players[_game.CurrentPlayerIndex]; if (currentPlayer.Money < 0) { await RegisterLoserAsync(currentPlayer); return; }
        _isAnimating = true; HasRolledThisTurn = true; var (die1, die2, total) = _game.RollDice(); EnqueueGroup("rolagem", new DialogueContext { Player = currentPlayer.Name }, true);
        await ShowDiceAnimationAsync(die1, die2); await AnimateForwardAsync(total); _preMovePlayerMoney = currentPlayer.Money; await _game.MoveCurrentPlayerAsync(total);
        AddDialogueTemplate("{PLAYER} avança {STEPS} casas.", new DialogueContext { Player = currentPlayer.Name, Steps = total }); await GameRepo.SaveGameAsync(GameId, _game);
        PrepareModalForLanding(currentPlayer); _isAnimating = false; StateHasChanged(); TriggerBotModalIfNeeded(currentPlayer); AdvanceDialogueIfIdle();
    }

    private async Task EndTurn()
    { if (_game is null || !CanEndTurn) return; HasRolledThisTurn = false; _game.NextTurn(); EnqueueGroup("transicao_turno", new DialogueContext { Player = _game.Players[_game.CurrentPlayerIndex].Name }, true); await GameRepo.SaveGameAsync(GameId, _game); AnnounceHumanTurnIfNeeded(); StateHasChanged(); AdvanceDialogueIfIdle(); await TryAutoRollForBotAsync(); }

    private async Task ShowDiceAnimationAsync(int finalDie1, int finalDie2)
    {
        try
        {
            var gifIndex = _rand.Next(1, 13); _rollingGifUrl = $"{Navigation.BaseUri}images/diceAnim/dice-rolling-{gifIndex}.gif"; _showDiceOverlay = true; StateHasChanged();
            var totalMs = _rand.Next(2000, 3001); var frameMs = 80; var elapsed = 0;
            while (elapsed < totalMs)
            { _diceFace1 = _rand.Next(1, 7); _diceFace2 = _rand.Next(1, 7); StateHasChanged(); var delay = Math.Min(frameMs, totalMs - elapsed); try { await Task.Delay(delay); } catch { break; } elapsed += delay; }
            _diceFace1 = Math.Clamp(finalDie1, 1, 6); _diceFace2 = Math.Clamp(finalDie2, 1, 6); StateHasChanged();
        }
        finally { _showDiceOverlay = false; _rollingGifUrl = string.Empty; StateHasChanged(); }
    }

    private async Task AnimateForwardAsync(int steps)
    { if (_game is null) return; var currentPlayer = _game.Players[_game.CurrentPlayerIndex]; var pos = currentPlayer.CurrentPosition; var track = GetTrackLength(); if (track <= 0) return; for (int i = 0; i < steps; i++) { pos = (pos + 1) % track; _pawnAnimPosition = pos; StateHasChanged(); try { await Task.Delay(_animStepMs); } catch { } } }

    private async Task AnimateBackwardAsync(int playerIndex, int steps)
    { if (_game is null || playerIndex < 0 || playerIndex >= _game.Players.Count) return; var prev = _isAnimating; _isAnimating = true; var player = _game.Players[playerIndex]; var pos = player.CurrentPosition; var track = GetTrackLength(); if (track <= 0) { _isAnimating = prev; return; } for (int i = 0; i < steps; i++) { pos = (pos - 1 + track) % track; _pawnAnimPosition = pos; StateHasChanged(); try { await Task.Delay(_animStepMs); } catch { } } player.CurrentPosition = pos; _pawnAnimPosition = -1; _isAnimating = prev; }
}
