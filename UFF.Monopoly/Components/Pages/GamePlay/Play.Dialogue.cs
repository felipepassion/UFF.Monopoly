using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase, IAsyncDisposable
{
    private double _gutterX = 0.0;
    private double _gutterY = 0.0;

    private readonly Random _rand = new();
    private readonly string[] _mouthFrames = { "/images/mr_monopoly/mr_monopoly_1.png", "/images/mr_monopoly/mr_monopoly_2.png", "/images/mr_monopoly/mr_monopoly_3.png", "/images/mr_monopoly/mr_monopoly_4.png" };
    private string _mouthImageUrl = "/images/mr_monopoly/mr_monopoly_1.png"; private int _mouthFrameIndex;
    private string _chatDisplay = string.Empty; private string _chatFull = string.Empty; private CancellationTokenSource? _typingCts; private int _typingDelayMs = 35; private bool _isTypingChat;
    private DialogueData? _dialogueData; private readonly Queue<string> _dialogueQueue = new(); private bool _dialogueInitialized; private static readonly Regex _placeholderRegex = new("{(?<key>[A-Z_]+)}", RegexOptions.Compiled);
    private string _chatMessage { get => _chatDisplay; set => _ = StartTypingAsync(value); }
    private bool _initialIntroDone;
    private CancellationTokenSource? _chatPauseCts;
    private CancellationTokenSource? _forcedAdvanceCts;
    private bool _awaitingForcedAdvance; private string _advanceHint = ">";
    private string _chatContainerStyle = string.Empty; private string _chatTextStyle = string.Empty;

    // Track last announced turn to avoid duplicate "it's your turn" messages
    private int _lastAnnouncedRound = -1;
    private int _lastAnnouncedPlayerIndex = -1;

    // Cancellation tied to modal/turn lifetimes
    private CancellationTokenSource? _modalCts;
    private CancellationTokenSource? _turnCts;

    // JS interop for dialogue advance
    private DotNetObjectReference<Play>? _dotNetRef;
    private bool _jsDialogueInitialized;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        // Initialize JS handlers to allow advancing dialogue via keyboard/click anywhere
        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("MonopolyDialogue.init", _dotNetRef);
            _jsDialogueInitialized = true;
        }
        catch { _jsDialogueInitialized = false; }
    }

    [JSInvokable]
    public async Task OnAdvanceRequestedAsync()
    {
        // Route JS events to the chat advance logic
        //await OnChatClicked();
    }

    // Dialogue busy indicator
    private bool IsDialogueBusy => _isTypingChat || _dialogueQueue.Count > 0;
    private async Task WaitForDialogueIdleAsync(int waitHintMs = 0, CancellationToken? externalToken = null)
    {
        var token = externalToken ?? _turnCts?.Token ?? _modalCts?.Token;
        if (waitHintMs > 0)
        {
            try { await Task.Delay(waitHintMs, token ?? CancellationToken.None); } catch { return; }
        }
        // Poll quickly until dialogue is fully idle
        int guardMs = 0;
        while (IsDialogueBusy)
        {
            if (token?.IsCancellationRequested == true) return;
            try { await Task.Delay(50, token ?? CancellationToken.None); } catch { return; }
            guardMs += 50;
            if (guardMs > 5000) break; // avoid stalling forever
        }
        Console.WriteLine($"[DIALOGUE] WaitForDialogueIdle done busy={IsDialogueBusy}");
    }

    private async Task LoadDialogueJsonAsync()
    { try { var baseUri = Navigation.BaseUri.TrimEnd('/'); var json = await Http.GetStringAsync($"{baseUri}/dialogues/monopoly-dialogues.json"); _dialogueData = JsonSerializer.Deserialize<DialogueData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new DialogueData(); } catch { _dialogueData = new DialogueData(); } }

    private void EnqueueGroup(string groupName, DialogueContext ctx, bool randomSingle = false, bool immediate = false)
    {
        if (_dialogueData?.Groups.TryGetValue(groupName, out var lines) != true || lines.Count == 0) return;
        if (groupName == "transicao_turno" && IsBotName(ctx.Player))
        { var botLines = new[] { "Agora é a minha vez.", "Assumo o turno." }; var chosenBot = botLines[_rand.Next(botLines.Length)]; AddDialogue(chosenBot, immediate); return; }
        if (randomSingle)
        { var chosen = lines[_rand.Next(lines.Count)]; AddDialogue(ApplyPlaceholders(chosen.Text, ctx), immediate); }
        else
        { foreach (var l in lines) AddDialogue(ApplyPlaceholders(l.Text, ctx), immediate); }
    }

    private static bool IsBotName(string? name) => !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name!, "^Bot \\d+", RegexOptions.IgnoreCase);

    private string ApplyPlaceholders(string? template, DialogueContext ctx)
    { if (string.IsNullOrWhiteSpace(template)) return string.Empty; return _placeholderRegex.Replace(template!, m => m.Groups["key"].Value switch { "PLAYER" => ctx.Player ?? string.Empty, "BLOCK" => ctx.Block ?? string.Empty, "AMOUNT" => ctx.Amount?.ToString() ?? string.Empty, "STEPS" => ctx.Steps?.ToString() ?? string.Empty, "DAYS" => ctx.Days?.ToString() ?? string.Empty, _ => m.Value }); }

    private void AddDialogueTemplate(string template, DialogueContext ctx, bool immediate = false) => AddDialogue(ApplyPlaceholders(template, ctx), immediate);

    private void AddDialogue(string raw, bool immediate = false)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        if (!immediate)
        { _dialogueQueue.Enqueue(raw); AdvanceDialogueIfIdle(); return; }
        // For immediate lines, do not clear queued lines if currently typing; just interrupt typing to finish the line.
        if (_isTypingChat)
        { try { _typingCts?.Cancel(); } catch { } _isTypingChat = false; }
        try { _chatPauseCts?.Cancel(); } catch { }
        _chatFull = raw; _chatDisplay = raw; _mouthFrameIndex = 0; _mouthImageUrl = _mouthFrames[_mouthFrameIndex]; StateHasChanged(); _ = StartChatPauseAsync();
    }

    private void AdvanceDialogueIfIdle() { if (_isTypingChat) return; if (_dialogueQueue.Count == 0) return; _chatMessage = _dialogueQueue.Dequeue(); }

    private async Task StartTypingAsync(string text)
    {
        _typingCts?.Cancel(); _typingCts = new CancellationTokenSource(); var token = _typingCts.Token;
        _isTypingChat = true; _awaitingForcedAdvance = false; _advanceHint = ">"; // digitando
        _chatFull = text ?? string.Empty; _chatDisplay = string.Empty; StateHasChanged();
        _ = AnimateMouthAsync(token);
        for (int i = 1; i <= _chatFull.Length; i++)
        { if (token.IsCancellationRequested) return; _chatDisplay = _chatFull[..i]; StateHasChanged(); try { await Task.Delay(_typingDelayMs, token); } catch { return; } }
        _isTypingChat = false; _mouthFrameIndex = 0; _mouthImageUrl = _mouthFrames[_mouthFrameIndex]; StateHasChanged(); _ = StartChatPauseAsync();
    }

    private async Task StartChatPauseAsync()
    {
        _chatPauseCts?.Cancel(); _chatPauseCts = new CancellationTokenSource(); var pauseToken = _chatPauseCts.Token;
        _advanceHint = ">>"; // terminou de escrever, aguardando próxima frase
        StateHasChanged();
        try { await Task.Delay(500, pauseToken); } catch { return; }
        if (pauseToken.IsCancellationRequested) return; OnChatFinished();
    }

    private async Task StartForcedAdvanceAsync()
    {
        // Primeiro clique inicia 1s e coloca estado de espera (>>). Segundo clique avança imediato.
        _forcedAdvanceCts?.Cancel(); _forcedAdvanceCts = new CancellationTokenSource(); var token = _forcedAdvanceCts.Token;
        _awaitingForcedAdvance = true; _advanceHint = ">>"; StateHasChanged();
        try { await Task.Delay(1000, token); } catch { return; }
        if (token.IsCancellationRequested) return; _awaitingForcedAdvance = false; _advanceHint = ">"; OnChatFinished();
    }

    private async Task AnimateMouthAsync(CancellationToken token)
    { while (_isTypingChat && !token.IsCancellationRequested) { _mouthFrameIndex = (_mouthFrameIndex + 1) % _mouthFrames.Length; _mouthImageUrl = _mouthFrames[_mouthFrameIndex]; StateHasChanged(); try { await Task.Delay(Math.Max(90, _typingDelayMs * 3), token); } catch { break; } } _mouthFrameIndex = 0; _mouthImageUrl = _mouthFrames[_mouthFrameIndex]; StateHasChanged(); }

    private async Task OnChatClicked()
    {
        // Se já está aguardando avanço forçado (segundo clique), avance imediatamente
        if (_awaitingForcedAdvance)
        {
            try { _forcedAdvanceCts?.Cancel(); } catch { }
            _awaitingForcedAdvance = false; _advanceHint = ">"; OnChatFinished();
            await Task.CompletedTask; return;
        }

        // Primeiro clique: se está digitando, mostrar texto completo e iniciar delay de 1s
        if (_isTypingChat)
        {
            try { _typingCts?.Cancel(); } catch { }
            _isTypingChat = false; _chatDisplay = _chatFull; _mouthFrameIndex = 0; _mouthImageUrl = _mouthFrames[_mouthFrameIndex]; StateHasChanged();
            _ = StartForcedAdvanceAsync(); await Task.CompletedTask; return;
        }
        // Não está digitando: inicia delay de 1s e passa para o próximo após esse tempo
        _ = StartForcedAdvanceAsync(); await Task.CompletedTask;
    }

    private void OnChatFinished()
    {
        // Avança automaticamente para próxima fala se houver itens na fila
        if (_dialogueQueue.Count > 0)
        {
            AdvanceDialogueIfIdle();
            return; // haverá nova digitação e nova pausa de 0.5s
        }
        // Quando a fila esvaziar pela primeira vez (intro), liberar HUD
        if (!_initialIntroDone && _dialogueInitialized && _dialogueQueue.Count == 0)
        {
            _initialIntroDone = true;
            _hudLocked = true; // lock HUD once shown and intro finished
            StateHasChanged();
        }
        // Ações pendentes que dependem do término da fala
        if (_pendingHumanRoll && !_isTypingChat)
        { _pendingHumanRoll = false; _ = InvokeAsync(async () => { await RollForCurrentPlayer(); }); }
        if (_pendingBotRoll && !_isTypingChat)
        { _pendingBotRoll = false; _ = InvokeAsync(async () => { await TryAutoRollForBotAsync(); }); }
    }

    private void AnnounceHumanTurnIfNeeded()
    {
        if (_game is null) return; if (!_initialIntroDone) return; if (!IsCurrentPlayerHuman()) return;
        // Avoid duplicate announcements for the same player and round
        if (_game.RoundCount == _lastAnnouncedRound && _game.CurrentPlayerIndex == _lastAnnouncedPlayerIndex) return;
        var playerName = _game.Players[_game.CurrentPlayerIndex].Name; var line = _humanTurnTaunts[_rand.Next(_humanTurnTaunts.Length)];
        AddDialogueTemplate(line, new DialogueContext { Player = playerName });
        _lastAnnouncedRound = _game.RoundCount; _lastAnnouncedPlayerIndex = _game.CurrentPlayerIndex;
    }

    private class DialogueData { public Dictionary<string, List<DialogueLine>> Groups { get; set; } = new(); }
    private class DialogueLine { public string Id { get; set; } = string.Empty; public string Text { get; set; } = string.Empty; }
    private class DialogueContext { public string? Player { get; set; } public string? Block { get; set; } public int? Amount { get; set; } public int? Steps { get; set; } public int? Days { get; set; } }

    public async ValueTask DisposeAsync()
    {
        try { _typingCts?.Cancel(); } catch { }
        try { _chatPauseCts?.Cancel(); } catch { }
        // Dispose JS handlers
        try
        {
            if (_jsDialogueInitialized)
                await JSRuntime.InvokeVoidAsync("MonopolyDialogue.dispose");
        }
        catch { }
        _dotNetRef?.Dispose();
        await Task.CompletedTask;
    }
}
