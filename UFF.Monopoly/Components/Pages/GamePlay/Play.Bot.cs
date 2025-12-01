using Microsoft.AspNetCore.Components;
using UFF.Monopoly.Entities;
using UFF.Monopoly.Components.Pages.BoardBuilders;
using UFF.Monopoly.Infrastructure.Bot;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase, IAsyncDisposable
{
    [Inject] private IBotDecisionService BotDecision { get; set; } = default!;

    private static bool IsBotPlayer(Player? p) => p?.Name?.StartsWith("Bot ", System.StringComparison.OrdinalIgnoreCase) == true;
    private bool IsCurrentPlayerHuman()
        => _game is not null && !_game.IsFinished && _game.CurrentPlayerIndex >= 0 && _game.CurrentPlayerIndex < _game.Players.Count && !BotDecision.IsBotPlayer(_game.Players[_game.CurrentPlayerIndex]);
    private bool IsCurrentPlayerBot()
        => _game is not null && !_game.IsFinished && _game.CurrentPlayerIndex >= 0 && _game.CurrentPlayerIndex < _game.Players.Count && BotDecision.IsBotPlayer(_game.Players[_game.CurrentPlayerIndex]);

    private CancellationTokenSource? _botCts;
    private readonly Queue<DecisionResult> _botQueue = new();

    private DecisionContext BuildDecisionContext(Block? block = null) => new()
    {
        Game = _game,
        CurrentPlayer = _game is not null && _game.CurrentPlayerIndex >= 0 && _game.CurrentPlayerIndex < _game.Players.Count ? _game.Players[_game.CurrentPlayerIndex] : null,
        CurrentBlock = block,
        HasRolledThisTurn = HasRolledThisTurn,
        IsTypingChat = _isTypingChat,
        IsAnimating = _isAnimating,
        ModalFromMove = _modalFromMove
    };

    private void EnqueueBotDecisions(IEnumerable<DecisionResult> decisions)
    { foreach (var d in decisions) _botQueue.Enqueue(d); }

    private void StartBotTurnIfNeeded(string origin)
    {
        if (_game is null) return;
        if (!IsCurrentPlayerBot()) return;
        if (_botQueue.Count > 0) return; // já há decisões pendentes
        var ctx = BuildDecisionContext();
        var d = BotDecision.EvaluateTurnStart(ctx);
        _botQueue.Enqueue(d);
        Console.WriteLine($"[BOT] StartBotTurnIfNeeded origin={origin} player={ctx.CurrentPlayer?.Name} decision={d.Type} reason={d.Reason}");
        _ = InvokeAsync(async () => await ProcessNextBotDecisionAsync());
    }

    private async Task ProcessNextBotDecisionAsync()
    {
        if (_game is null) return;
        if (_botQueue.Count == 0) return;
        if (!IsCurrentPlayerBot()) { _botQueue.Clear(); return; }

        var decision = _botQueue.Dequeue();
        _botCts?.Cancel();
        _botCts = new CancellationTokenSource();
        var token = _botCts.Token;
        try { if (decision.SuggestedDelayMs > 0) await Task.Delay(decision.SuggestedDelayMs, token); } catch { return; }
        if (token.IsCancellationRequested) return;

        await ExecuteBotDecisionAsync(decision);
        if (_botQueue.Count > 0) await ProcessNextBotDecisionAsync();
    }

    private async Task ExecuteBotDecisionAsync(DecisionResult decision)
    {
        if (!IsCurrentPlayerBot()) { _botQueue.Clear(); return; }
        Console.WriteLine($"[BOT] Execute decision={decision.Type} player={_game?.Players[_game.CurrentPlayerIndex].Name} reason={decision.Reason}");
        switch (decision.Type)
        {
            case DecisionType.Roll:
                await RollAndMove();
                if (!_showBlockModal)
                {
                    HasRolledThisTurn = true;
                    _botQueue.Clear();
                    _botQueue.Enqueue(DecisionResult.Simple(DecisionType.EndTurn, "Turno concluído (sem modal)", 5, 600));
                    await ProcessNextBotDecisionAsync();
                }
                break;
            case DecisionType.Buy:
                if (_modalBlock is not null && _modalPlayer is not null)
                {
                    // pequena pausa antes da ação para humanizar
                    try { await Task.Delay(300); } catch { }
                    if (_game!.TryBuyProperty(_modalPlayer, _modalBlock))
                    {
                        SyncOwnersToBoardSpaces(); StateHasChanged(); await GameRepo.SaveGameAsync(GameId, _game);
                        // toast de compra
                        await ShowActionToastAsync($"{_modalPlayer.Name} comprou {_modalBlock.Name}", 1800);
                    }
                    // pequena pausa para deixar o usuário perceber a compra
                    try { await Task.Delay(350); } catch { }
                }
                // sempre fechar modal após ação
                if (_showBlockModal) { await CloseBlockModal(); }
                // e prosseguir para fim de turno com pequena folga
                _botQueue.Enqueue(DecisionResult.Simple(DecisionType.EndTurn, "Após compra", 3, 500));
                break;
            case DecisionType.Upgrade:
                if (_modalBlock is PropertyBlock pb && _modalPlayer is not null && pb.Owner == _modalPlayer && CanUpgradeAllowed(pb))
                {
                    try { await Task.Delay(280); } catch { }
                    if (pb.Upgrade(_modalPlayer))
                    {
                        if (pb.BuildingType != BuildingType.None && pb.BuildingLevel > 0)
                        { var evo = BuildingEvolutionDescriptions.Get(pb.BuildingType, Math.Clamp(pb.BuildingLevel, 1, 4)); pb.Name = evo.Name; }
                        _modalPlayer.LastBuildTurn = _game!.RoundCount;
                        SyncOwnersToBoardSpaces(); StateHasChanged(); await GameRepo.SaveGameAsync(GameId, _game);
                        try { await Task.Delay(320); } catch { }
                    }
                }
                if (_showBlockModal) { await CloseBlockModal(); }
                _botQueue.Enqueue(DecisionResult.Simple(DecisionType.EndTurn, "Após upgrade", 3, 500));
                break;
            case DecisionType.EndTurn:
                // garantir que modal esteja fechado
                if (_showBlockModal) { await CloseBlockModal(); }
                _game!.NextTurn(); HasRolledThisTurn = false; await GameRepo.SaveGameAsync(GameId, _game);
                _botQueue.Clear();
                EnqueueGroup("transicao_turno", new DialogueContext { Player = _game.Players[_game.CurrentPlayerIndex].Name }, true);
                AnnounceHumanTurnIfNeeded(); StateHasChanged(); AdvanceDialogueIfIdle();
                StartBotTurnIfNeeded("after_end_turn");
                break;
            case DecisionType.Skip:
                _botQueue.Enqueue(DecisionResult.Simple(DecisionType.Roll, "Retry roll", 8, 500));
                break;
            case DecisionType.None:
            default:
                break;
        }
    }

    private async Task TryAutoRollForBotAsync()
    { StartBotTurnIfNeeded("auto_roll_trigger"); await Task.CompletedTask; }

    private void TriggerBotModalIfNeeded(Player currentPlayer)
    {
        if (!BotDecision.IsBotPlayer(currentPlayer)) return;
        _botHasActedThisModal = false;
        _ = InvokeAsync(async () => { try { await Task.Delay(400); } catch { } await BotActOnModalAsync(); });
    }

    private async Task BotActOnModalAsync()
    {
        if (_game is null || _modalPlayer is null) return;
        if (!BotDecision.IsBotPlayer(_modalPlayer) || _botHasActedThisModal) return;
        _botHasActedThisModal = true;
        var ctx = BuildDecisionContext(_modalBlock);
        var decisions = BotDecision.EvaluateModal(ctx);
        _botQueue.Clear(); EnqueueBotDecisions(decisions);
        await ProcessNextBotDecisionAsync();
    }

    private async Task BotAutoActionsIfNeeded()
    {
        if (_game is null || _modalPlayer is null) return;
        if (!BotDecision.IsBotPlayer(_modalPlayer) || _botHasActedThisModal) return;
        _botHasActedThisModal = true;
        var ctx = BuildDecisionContext(_modalBlock);
        var decisions = BotDecision.EvaluateModal(ctx);
        _botQueue.Clear(); EnqueueBotDecisions(decisions);
        await ProcessNextBotDecisionAsync();
    }

    private async Task TryBotPurchaseAsync() { }
    private async Task TryBotUpgradeAsync() { }
}
