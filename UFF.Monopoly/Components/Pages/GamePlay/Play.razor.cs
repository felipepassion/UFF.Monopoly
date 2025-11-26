using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System.Text.Json;
using UFF.Monopoly.Data;
using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Entities;
using UFF.Monopoly.Models;
using UFF.Monopoly.Repositories;
using System.Net.Http;
using System.Threading;
using System.Text.RegularExpressions;
using UFF.Monopoly.Constants; // cores players
using UFF.Monopoly.Components.Pages.BoardBuilders; // evolution info

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase, IAsyncDisposable
{
    // Injeções
    [Inject] public IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;
    [Inject] public IGameRepository GameRepo { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public Infrastructure.IUserProfileService Profiles { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public HttpClient Http { get; set; } = default!;

    // Parâmetros
    [Parameter] public Guid GameId { get; set; }
    [SupplyParameterFromQuery] public Guid? boardId { get; set; }
    [SupplyParameterFromQuery(Name = "humanCount")] public int? HumanCountQuery { get; set; }
    [SupplyParameterFromQuery] public string? pawns { get; set; }

    // Estado principal
    private Game? _game; private bool _loading = true;
    private List<BoardSpaceDto> BoardSpaces { get; set; } = new();
    private Dictionary<int, BlockTemplateEntity> _templatesByPosition = new();

    // Board layout
    private int Rows; private int Cols; private int CellSize; private List<(int r, int c)> Perimeter = new();
    // Escala do board (antes era constante 1.5). Agora dinâmica para respeitar exatamente o CellSize configurado.
    // Usamos 1.0 para aplicar o tamanho salvo sem multiplicar.
    private double _boardScale = 1.0; private string _boardWidthCss = "0px"; private string _boardHeightCss = "0px";

    // Peões
    private string PawnUrl = "/images/pawns/PawnsB1.png";
    private List<int> _pawnsForPlayers = new();
    private int _pawnAnimPosition = -1;

    // Animações
    private bool _isAnimating; private int _animStepMs = 140; private bool _showDiceOverlay; private int _diceFace1 = 1; private int _diceFace2 = 1; private string _rollingGifUrl = string.Empty;

    // Modais / ação de bloco
    private bool _showBlockModal; private Block? _modalBlock; private Player? _modalPlayer; private bool _modalFromMove; private BlockTemplateEntity? _modalTemplateEntity; private int _preMovePlayerMoney;
    private PendingActionKind _pendingActionKind = PendingActionKind.None; private int _pendingAmount; private int _pendingBackSteps;

    // Vitória / derrota
    private bool _showWinnerModal; private string _winnerName = string.Empty; private bool _showLoserModal; private string _loserName = string.Empty;

    // Bot toast
    private bool _showBotActionToast; private string _botActionMessage = string.Empty; private bool _botHasActedThisModal;

    // Estados de turno (HUD e botões)
    private bool HasRolledThisTurn; // se jogador humano já rolou dados neste turno
    // Flags de ações pendentes enquanto chat digita
    private bool _pendingHumanRoll; private bool _pendingBotRoll;

    // Helper para classificar humano/bot sem depender da ordem
    private static bool IsBotPlayer(Player? p) => p?.Name?.StartsWith("Bot ", StringComparison.OrdinalIgnoreCase) == true;
    private bool IsCurrentPlayerHuman()
        => _game is not null && !_game.IsFinished && _game.CurrentPlayerIndex >= 0 && _game.CurrentPlayerIndex < _game.Players.Count && !IsBotPlayer(_game.Players[_game.CurrentPlayerIndex]);
    private bool IsCurrentPlayerBot()
        => _game is not null && !_game.IsFinished && _game.CurrentPlayerIndex >= 0 && _game.CurrentPlayerIndex < _game.Players.Count && IsBotPlayer(_game.Players[_game.CurrentPlayerIndex]);

    private bool IsPlayerTurn => IsCurrentPlayerHuman();
    private bool CanRollDice => IsPlayerTurn && !HasRolledThisTurn && !_isAnimating && !_showBlockModal && !_showWinnerModal && !_showLoserModal && !_isTypingChat; // adiciona checagem de chat
    private bool IsDiceAnimating => _isAnimating || _showDiceOverlay;
    private bool CanEndTurn => IsPlayerTurn && HasRolledThisTurn && !_isAnimating && !_showBlockModal && !_showWinnerModal && !_showLoserModal;

    // Diálogo / chat
    private readonly Random _rand = new();
    private readonly string[] _mouthFrames = { "/images/mr_monopoly/mr_monopoly_1.png", "/images/mr_monopoly/mr_monopoly_2.png", "/images/mr_monopoly/mr_monopoly_3.png", "/images/mr_monopoly/mr_monopoly_4.png" };
    private string _mouthImageUrl = "/images/mr_monopoly/mr_monopoly_1.png"; private int _mouthFrameIndex;
    private string _chatDisplay = string.Empty; private string _chatFull = string.Empty; private CancellationTokenSource? _typingCts; private int _typingDelayMs = 35; private bool _isTypingChat;
    private DialogueData? _dialogueData; private readonly Queue<string> _dialogueQueue = new(); private bool _dialogueInitialized; private static readonly Regex _placeholderRegex = new("{(?<key>[A-Z_]+)}", RegexOptions.Compiled);
    private string _chatMessage { get => _chatDisplay; set => _ = StartTypingAsync(value); }
    private bool _initialIntroDone; // bloqueia HUD até terminar falas de inicio

    // Estilos calculados dinamicamente
    private string _centerCharStyle = string.Empty; private string _chatContainerStyle = string.Empty; private string _chatTextStyle = string.Empty;
    private string _playersHudStyle = string.Empty; private string _turnActionsStyle = string.Empty; // novos estilos calculados

    // Adicionar campo para imagem de fundo do tabuleiro (centro)
    private string? _boardCenterImageUrl;
    // Paleta simples para cores de overlay por jogador (substituída por PlayerColors)
    private readonly string[] _ownerColors = PlayerColors.Colors;

    // Taunts do Mr. Monopoly para quando for a vez do humano
    private readonly string[] _humanTurnTaunts = new[]
    {
        "Sua vez, {PLAYER}! Vamos ver se você consegue algo além de pagar aluguel.",
        "Sua vez, {PLAYER}. Capricha no dado... eu adoro quando você erra!",
        "É agora, {PLAYER}. Mostra serviço ou deixa que eu ensino como se joga.",
        "Sua vez! Se prepara pra tomar renda, {PLAYER}.",
        "Sua vez, {PLAYER}. Prometo pegar leve... mentira." ,
        "Vai lá, {PLAYER}. Quanto mais você anda, mais você me deve." 
    };

    protected override async Task OnParametersSetAsync() => await InitializeAsync();

    private async Task InitializeAsync()
    {
        _loading = true; HasRolledThisTurn = false;
        if (!boardId.HasValue) { Navigation.NavigateTo("/local"); return; }
        await LoadBoardLayoutAsync(boardId.Value);
        await LoadDialogueJsonAsync();
        _game = await GameRepo.GetGameAsync(GameId);
        if (_game is not null)
        {
            _pawnAnimPosition = _game.Players.FirstOrDefault()?.CurrentPosition ?? 0;
            _pawnsForPlayers = _game.Players.Select(p => Math.Clamp(p.PawnIndex, 1, 6)).ToList();
        }
        if (!_pawnsForPlayers.Any()) ParsePawnsQuery();
        try { var pawn = await Profiles.GetPawnFromSessionAsync(); if (!string.IsNullOrWhiteSpace(pawn)) PawnUrl = pawn; } catch { }
        if (_game is not null) { SyncOwnersToBoardSpaces(); }
        if (!_dialogueInitialized)
        {
            var firstPlayerName = _game?.Players.FirstOrDefault()?.Name ?? "Jogador";
            EnqueueGroup("inicio", new DialogueContext { Player = firstPlayerName });
            AddDialogue("Eu sou o Sr. Monopoly. Só começam depois das minhas provocações. Mostrem que não vão falir tão rápido!");
            _dialogueInitialized = true;
            _initialIntroDone = false; // bloquear HUD até terminar
        }
        // Anuncia caso já seja a vez do humano na carga inicial (só será exibido depois da intro)
        AnnounceHumanTurnIfNeeded();
        AdvanceDialogueIfIdle();
        _loading = false; StateHasChanged();
        _ = TryAutoRollForBotAsync();
    }

    private void SyncOwnersToBoardSpaces()
    {
        if (_game is null) return;
        foreach (var space in BoardSpaces)
        {
            var parts = (space.Id ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || !int.TryParse(parts[1], out var pos)) continue;
            var block = _game.Board.FirstOrDefault(b => b.Position == pos);
            if (block?.Owner is null)
            {
                space.OwnerPlayerIndex = null;
            }
            else
            {
                var idx = GetPlayerIndex(block.Owner.Id);
                space.OwnerPlayerIndex = idx >= 0 ? idx : null;
            }
            if (block is PropertyBlock pb)
            {
                // Garantir que nome evolui conforme BuildingType/Level
                if (pb.BuildingType != BuildingType.None && pb.BuildingLevel > 0)
                {
                    var evo = BuildingEvolutionDescriptions.Get(pb.BuildingType, Math.Clamp(pb.BuildingLevel, 1, 4));
                    pb.Name = evo.Name; // atualiza nome do bloco
                }
                space.Name = pb.Name; // refletir no espaço
                space.BuildingType = pb.BuildingType;
                space.BuildingLevel = pb.BuildingLevel;
                space.ImageUrl = string.IsNullOrWhiteSpace(pb.ImageUrl) ? space.ImageUrl : pb.ImageUrl;
            }
        }
    }

    // ===== Layout do tabuleiro =====
    private async Task LoadBoardLayoutAsync(Guid bId)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var board = await db.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bId);
        if (board is null) return;
        Rows = board.Rows; Cols = board.Cols; CellSize = board.CellSizePx; Perimeter = BuildPerimeterClockwise(Rows, Cols);
        // Ajustar escala: se célula muito pequena, aumentar um pouco para legibilidade, senão usar 1.0
        _boardScale = CellSize switch
        {
            < 50 => 1.5,
            < 70 => 1.3,
            < 90 => 1.15,
            _ => 1.0
        };
        _boardCenterImageUrl = board.CenterImageUrl;
        var templates = await db.BlockTemplates.AsNoTracking().Where(t => t.BoardDefinitionId == bId).OrderBy(t => t.Position).ToListAsync();

        // Se o número de blocos (templates) não bate com o perímetro calculado,
        // ajustamos o perímetro para o menor comprimento para evitar "teleporte" na animação.
        if (templates.Count > 0 && Perimeter.Count != templates.Count)
        {
            var effective = Math.Min(Perimeter.Count, templates.Count);
            Perimeter = Perimeter.Take(effective).ToList();
        }

        _templatesByPosition = templates.ToDictionary(t => t.Position, t => t);
        BoardSpaces = new List<BoardSpaceDto>(Perimeter.Count);
        for (int i = 0; i < Perimeter.Count; i++)
        {
            var (r, c) = Perimeter[i]; var template = templates.ElementAtOrDefault(i);
            var img = template != null && !string.IsNullOrWhiteSpace(template.ImageUrl) ? template.ImageUrl : GetImageForType(template?.Type ?? BlockType.Property);
            BoardSpaces.Add(new BoardSpaceDto
            {
                Id = $"space-{i}", Name = template?.Name ?? $"Space {i}", ImageUrl = img, Price = template?.Price ?? 0, Rent = template?.Rent ?? 0,
                Type = template?.Type ?? BlockType.Property, BuildingType = template?.BuildingType ?? BuildingType.None, BuildingLevel = template?.Level is null ? 0 : (int)template.Level,
                Style = new BoardSpaceStyle { Top = $"{(int)(r * CellSize * _boardScale)}px", Left = $"{(int)(c * CellSize * _boardScale)}px", Width = $"{(int)(CellSize * _boardScale)}px", Height = $"{(int)(CellSize * _boardScale)}px" }
            });
        }
        _boardWidthCss = ((int)(Cols * CellSize * _boardScale)) + "px"; _boardHeightCss = ((int)(Rows * CellSize * _boardScale)) + "px";
        // Centro / chat ajustes (reduz tamanho do personagem e fonte do chat)
        var cellScaled = (int)(CellSize * _boardScale);
        var imgHeight = (int)(cellScaled * 1.7); // antes 2.0 -> menor
        var bottomRowTop = (int)((Rows - 1) * cellScaled);
        var top = bottomRowTop - imgHeight - (int)(cellScaled * 0.3); if (top < 0) top = 0;
        var estimatedWidth = (int)(imgHeight * 0.75); // levemente menor
        var rightColLeft = (Cols - 1) * cellScaled; var marginChar = (int)(cellScaled * 0.15);
        var left = rightColLeft - estimatedWidth - marginChar; if (left < marginChar) left = marginChar;
        _centerCharStyle = $"position:absolute;top:{top}px;left:{left}px;height:{imgHeight}px;z-index:1500;pointer-events:none;filter:drop-shadow(0 12px 24px rgba(0,0,0,0.6));";
        if (Rows >= 3 && Cols >= 3)
        {
            var interiorLeft = cellScaled; var interiorTop = cellScaled; var interiorWidth = (Cols - 2) * cellScaled; var interiorHeight = (Rows - 2) * cellScaled; var margin = (int)(cellScaled * 0.2);
            var chatHeight = Math.Min((int)(cellScaled * 1.6), Math.Max(cellScaled, (int)(interiorHeight * 0.42))); // ligeiro ajuste
            var chatTop = interiorTop + interiorHeight - chatHeight - margin;
            _chatContainerStyle = $"position:absolute;left:{interiorLeft}px;top:{chatTop}px;width:{interiorWidth}px;height:{chatHeight}px;background:url('/images/mr_monopoly/conversation-container.png') center/100% 100% no-repeat;z-index:1200;pointer-events:none;";
            var textPadLeft = (int)(cellScaled * 0.25); var textPadRight = (int)(cellScaled * 1.8); var textPadTop = (int)(cellScaled * 0.23); var textPadBottom = (int)(cellScaled * 0.2); // top antes 0.15 -> 0.23
            var fontSize = Math.Max(9, (int)(cellScaled * 0.16)); // reduzido significativamente para 0.16
            _chatTextStyle = $"position:absolute;left:{textPadLeft}px;top:{textPadTop}px;right:{textPadRight}px;bottom:{textPadBottom}px;font-size:{fontSize}px;line-height:1.15;color:#d4e9dc;font-family:'Segoe UI',sans-serif;font-weight:600;text-shadow:0 2px 4px rgba(0,0,0,0.7);overflow:hidden;display:flex;align-items:flex-start;word-wrap:break-word;";

            var hudWidth = Math.Min(interiorWidth, (int)(cellScaled * 14));
            var hudLeft = interiorLeft + (interiorWidth - hudWidth) / 2;
            var hudBottomGap = (int)(cellScaled * 0.15);
            var hudHeight = (int)(cellScaled * 2.1); // ligeiro ajuste
            var hudTop = chatTop - hudHeight - hudBottomGap; if (hudTop < interiorTop) hudTop = interiorTop + margin;
            _playersHudStyle = $"left:{hudLeft}px;top:{hudTop}px;width:{hudWidth}px;z-index:1600;";
            var actionsWidth = Math.Min((int)(cellScaled * 6.2), interiorWidth);
            var actionsLeft = interiorLeft + (interiorWidth - actionsWidth) / 2;
            var actionsTop = hudTop + hudHeight + (int)(cellScaled * 0.05);
            _turnActionsStyle = $"position:absolute;left:{actionsLeft}px;top:{actionsTop}px;width:{actionsWidth}px;display:flex;justify-content:center;gap:12px;z-index:1650;";
        }
        else
        {
            var chatHeightSmall = (int)(cellScaled * 2.0);
            _chatContainerStyle = $"position:absolute;left:0;bottom:0;width:{Cols * cellScaled}px;height:{chatHeightSmall}px;background:url('/images/mr_monopoly/conversation-container.png') center/100% 100% no-repeat;z-index:1200;pointer-events:none;";
            _chatTextStyle = $"position:absolute;left:{(int)(cellScaled * 0.2)}px;top:{(int)(cellScaled * 0.22)}px;right:{(int)(cellScaled * 1.8)}px;bottom:{(int)(cellScaled * 0.3)}px;font-size:{Math.Max(8, (int)(cellScaled * 0.28))}px;line-height:1.15;color:#d4e9dc;font-family:'Segoe UI',sans-serif;font-weight:600;text-shadow:0 2px 4px rgba(0,0,0,0.7);overflow:hidden;display:flex;align-items:flex-start;word-wrap:break-word;"; // top antes 0.15 -> 0.22
            // Simplified HUD placement for small boards
            _playersHudStyle = $"left:0;top:0;width:{Cols * cellScaled}px;z-index:1600;";
            _turnActionsStyle = $"position:absolute;left:0;top:{(int)(cellScaled * 2.3)}px;width:{Cols * cellScaled}px;display:flex;justify-content:center;gap:12px;z-index:1650;";
        }
    }

    // ===== Diálogo =====
    private async Task LoadDialogueJsonAsync()
    { try { var baseUri = Navigation.BaseUri.TrimEnd('/'); var json = await Http.GetStringAsync($"{baseUri}/dialogues/monopoly-dialogues.json"); _dialogueData = JsonSerializer.Deserialize<DialogueData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new DialogueData(); } catch { _dialogueData = new DialogueData(); } }
    private void EnqueueGroup(string groupName, DialogueContext ctx, bool randomSingle = false, bool immediate = false)
    {
        if (_dialogueData?.Groups.TryGetValue(groupName, out var lines) != true || lines.Count == 0) return;
        // Se transição de turno e jogador for bot, usar falas em primeira pessoa customizadas
        if (groupName == "transicao_turno" && IsBotName(ctx.Player))
        {
            var botLines = new[] { "Agora é a minha vez.", "Assumo o turno." };
            var chosenBot = botLines[_rand.Next(botLines.Length)];
            AddDialogue(chosenBot, immediate);
            return;
        }
        if (randomSingle)
        {
            var chosen = lines[_rand.Next(lines.Count)];
            AddDialogue(ApplyPlaceholders(chosen.Text, ctx), immediate);
        }
        else
        {
            foreach (var l in lines)
                AddDialogue(ApplyPlaceholders(l.Text, ctx), immediate);
        }
    }

    private static bool IsBotName(string? name) => !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name!, "^Bot \\d+", RegexOptions.IgnoreCase);

    private string ApplyPlaceholders(string? template, DialogueContext ctx)
    { if (string.IsNullOrWhiteSpace(template)) return string.Empty; return _placeholderRegex.Replace(template!, m => m.Groups["key"].Value switch { "PLAYER" => ctx.Player ?? string.Empty, "BLOCK" => ctx.Block ?? string.Empty, "AMOUNT" => ctx.Amount?.ToString() ?? string.Empty, "STEPS" => ctx.Steps?.ToString() ?? string.Empty, "DAYS" => ctx.Days?.ToString() ?? string.Empty, _ => m.Value }); }
    private void AddDialogueTemplate(string template, DialogueContext ctx, bool immediate = false) => AddDialogue(ApplyPlaceholders(template, ctx), immediate);
    private void AddDialogue(string raw, bool immediate = false)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        if (!immediate)
        {
            _dialogueQueue.Enqueue(raw);
            AdvanceDialogueIfIdle();
            return;
        }
        // Interrupção imediata: corta fala atual e mostra esta agora.
        if (_isTypingChat)
        {
            try { _typingCts?.Cancel(); } catch { }
            _isTypingChat = false;
        }
        // Limpa fila antiga para não exibir mensagens atrasadas (ex: "avança X casas" após upgrade).
        _dialogueQueue.Clear();
        _chatFull = raw;
        _chatDisplay = raw; // mostra inteira sem digitação
        _mouthFrameIndex = 0; _mouthImageUrl = _mouthFrames[_mouthFrameIndex];
        StateHasChanged();
        OnChatFinished();
    }
    private void AdvanceDialogueIfIdle() { if (_isTypingChat) return; if (_dialogueQueue.Count == 0) return; _chatMessage = _dialogueQueue.Dequeue(); }
    private async Task StartTypingAsync(string text)
    { _typingCts?.Cancel(); _typingCts = new CancellationTokenSource(); var token = _typingCts.Token; _isTypingChat = true; _chatFull = text ?? string.Empty; _chatDisplay = string.Empty; StateHasChanged(); _ = AnimateMouthAsync(token); for (int i = 1; i <= _chatFull.Length; i++) { if (token.IsCancellationRequested) return; _chatDisplay = _chatFull[..i]; StateHasChanged(); try { await Task.Delay(_typingDelayMs, token); } catch { return; } } _isTypingChat = false; _mouthFrameIndex = 0; _mouthImageUrl = _mouthFrames[_mouthFrameIndex]; StateHasChanged(); OnChatFinished(); }
    // Método de animação da boca (reintroduzido)
    private async Task AnimateMouthAsync(CancellationToken token)
    { while (_isTypingChat && !token.IsCancellationRequested) { _mouthFrameIndex = (_mouthFrameIndex + 1) % _mouthFrames.Length; _mouthImageUrl = _mouthFrames[_mouthFrameIndex]; StateHasChanged(); try { await Task.Delay(Math.Max(90, _typingDelayMs * 3), token); } catch { break; } } _mouthFrameIndex = 0; _mouthImageUrl = _mouthFrames[_mouthFrameIndex]; StateHasChanged(); }
    // Clique no chat (reintroduzido) agora dispara OnChatFinished se interromper
    private async Task OnChatClicked() { if (_isTypingChat) { _typingCts?.Cancel(); _isTypingChat = false; _chatDisplay = _chatFull; _mouthFrameIndex = 0; _mouthImageUrl = _mouthFrames[_mouthFrameIndex]; StateHasChanged(); OnChatFinished(); return; } AdvanceDialogueIfIdle(); await Task.CompletedTask; }
    // Chamado quando termino de digitar uma fala completa
    private void OnChatFinished()
    {
        // Se ainda há falas restantes da intro, avança automaticamente para a próxima
        if (!_initialIntroDone && _dialogueInitialized && _dialogueQueue.Count > 0)
        {
            // inicia próxima fala imediatamente
            AdvanceDialogueIfIdle();
            return; // aguarda finalizar próxima antes de liberar HUD
        }
        // Libera HUD após término completo da intro (todas as mensagens consumidas)
        if (!_initialIntroDone && _dialogueInitialized && _dialogueQueue.Count == 0)
        {
            _initialIntroDone = true; // agora HUD pode aparecer
            StateHasChanged();
        }
        // Se houver rolagem humana pendente
        if (_pendingHumanRoll && !_isTypingChat)
        {
            _pendingHumanRoll = false;
            _ = InvokeAsync(async () => { await RollForCurrentPlayer(); });
        }
        // Se houver rolagem automática de bot pendente
        if (_pendingBotRoll && !_isTypingChat)
        {
            _pendingBotRoll = false;
            _ = InvokeAsync(async () => { await TryAutoRollForBotAsync(); });
        }
    }

    // ===== Ações turno / dados =====
    private async Task RollForCurrentPlayer()
    { if (_game is null || _isAnimating || HasRolledThisTurn) return; if (!IsCurrentPlayerHuman()) return; if (_isTypingChat) { _pendingHumanRoll = true; return; } await RollAndMove(); }
    private async Task RollAndMove()
    { if (_game is null || _isAnimating) return; var currentPlayer = _game.Players[_game.CurrentPlayerIndex]; if (currentPlayer.Money < 0) { await RegisterLoserAsync(currentPlayer); return; } _isAnimating = true; HasRolledThisTurn = true; var (die1, die2, total) = _game.RollDice(); EnqueueGroup("rolagem", new DialogueContext { Player = currentPlayer.Name }, true); await ShowDiceAnimationAsync(die1, die2); await AnimateForwardAsync(total); _preMovePlayerMoney = currentPlayer.Money; await _game.MoveCurrentPlayerAsync(total); AddDialogueTemplate("{PLAYER} avança {STEPS} casas.", new DialogueContext { Player = currentPlayer.Name, Steps = total }); await GameRepo.SaveGameAsync(GameId, _game); PrepareModalForLanding(currentPlayer); _isAnimating = false; StateHasChanged(); TriggerBotModalIfNeeded(currentPlayer); AdvanceDialogueIfIdle(); }
    private async Task EndTurn() { if (_game is null || !CanEndTurn) return; HasRolledThisTurn = false; _game.NextTurn(); EnqueueGroup("transicao_turno", new DialogueContext { Player = _game.Players[_game.CurrentPlayerIndex].Name }, true); await GameRepo.SaveGameAsync(GameId, _game); AnnounceHumanTurnIfNeeded(); StateHasChanged(); AdvanceDialogueIfIdle(); await TryAutoRollForBotAsync(); }

    // ===== Bot =====
    private void TriggerBotModalIfNeeded(Player currentPlayer)
    { if (IsBotPlayer(currentPlayer)) { _botHasActedThisModal = false; _ = InvokeAsync(async () => { try { await Task.Delay(150); } catch { } await BotActOnModalAsync(); }); } }
    private async Task TryAutoRollForBotAsync()
    { 
        if (_game is null) return; 
        if (_isTypingChat) { if (IsCurrentPlayerBot()) _pendingBotRoll = true; return; } 
        if (IsCurrentPlayerBot() && !_isAnimating && !HasRolledThisTurn) 
        { 
            try { await Task.Delay(700); } catch { } 
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
        { 
            AnnounceHumanTurnIfNeeded(); 
        } 
    }
    private async Task TriggerBotTurnEndAsync() { if (_game is null) return; HasRolledThisTurn = false; _game.NextTurn(); EnqueueGroup("transicao_turno", new DialogueContext { Player = _game.Players[_game.CurrentPlayerIndex].Name }, true); await GameRepo.SaveGameAsync(GameId, _game); AnnounceHumanTurnIfNeeded(); StateHasChanged(); AdvanceDialogueIfIdle(); await TryAutoRollForBotAsync(); }
    private async Task BotActOnModalAsync() { if (_game is null || _modalPlayer is null || !_modalFromMove) return; var idx = GetPlayerIndex(_modalPlayer.Id); if (!IsBotPlayer(_modalPlayer) || _botHasActedThisModal) return; _botHasActedThisModal = true; EnqueueGroup("bot_pensando", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock?.Name }, true); AdvanceDialogueIfIdle(); try { await Task.Delay(900); } catch { } await TryBotPurchaseAsync(); try { await Task.Delay(600); } catch { } await TryBotUpgradeAsync(); try { await Task.Delay(900); } catch { } await CloseBlockModal(); }
    private async Task BotAutoActionsIfNeeded() { if (_game is null || _modalPlayer is null) return; if (!IsBotPlayer(_modalPlayer) || _botHasActedThisModal) return; EnqueueGroup("bot_pensando", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock?.Name }, true); AdvanceDialogueIfIdle(); try { await Task.Delay(700); } catch { } await TryBotPurchaseAsync(); try { await Task.Delay(500); } catch { } await TryBotUpgradeAsync(); }
    private async Task TryBotPurchaseAsync() { if (_modalBlock is not null && _modalBlock.Owner is null && (_modalBlock.Type == BlockType.Property || _modalBlock.Type == BlockType.Company) && _modalPlayer!.Money >= _modalBlock.Price) { EnqueueGroup("bot_acao_compra", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock.Name }, true, immediate: true); try { await Task.Delay(700); } catch { } _game!.TryBuyProperty(_modalPlayer, _modalBlock); await GameRepo.SaveGameAsync(GameId, _game); SyncOwnersToBoardSpaces(); StateHasChanged(); EnqueueGroup("acao_compra", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock.Name }, true, immediate: true); } }
    private async Task TryBotUpgradeAsync() { if (_modalBlock is PropertyBlock pb && pb.Owner == _modalPlayer && CanUpgradeAllowed(pb)) { EnqueueGroup("bot_acao_upgrade", new DialogueContext { Player = _modalPlayer!.Name, Block = pb.Name }, true, immediate: true); try { await Task.Delay(650); } catch { } if (pb.Upgrade(_modalPlayer!)) { if (pb.BuildingType != BuildingType.None && pb.BuildingLevel > 0) { var evo = BuildingEvolutionDescriptions.Get(pb.BuildingType, Math.Clamp(pb.BuildingLevel,1,4)); pb.Name = evo.Name; } _modalPlayer!.LastBuildTurn = _game!.RoundCount; await GameRepo.SaveGameAsync(GameId, _game); SyncOwnersToBoardSpaces(); StateHasChanged(); EnqueueGroup("acao_upgrade", new DialogueContext { Player = _modalPlayer.Name, Block = pb.Name, Amount = pb.BuildingLevel }, true, immediate: true); } } }

    // ===== Modal de bloco =====
    private void PrepareModalForLanding(Player currentPlayer)
    { if (_game is null) return; var landed = _game.Board.FirstOrDefault(b => b.Position == currentPlayer.CurrentPosition); ResetPendingSpecial(); if (landed is not null) { if (landed.Type == BlockType.Tax) ConfigureTax(landed, currentPlayer); else if (landed.Type == BlockType.Chance) ConfigureChance(); else if (landed.Type == BlockType.Reves) ConfigureReves(); } _modalPlayer = currentPlayer; _modalBlock = landed; _modalTemplateEntity = _templatesByPosition.TryGetValue(currentPlayer.CurrentPosition, out var tpl) ? tpl : null; _pawnAnimPosition = -1; _modalFromMove = true; _showBlockModal = true; }
    private async Task CloseBlockModal()
    { 
        _showBlockModal = false; 
        if (_modalFromMove && _game is not null && _modalPlayer is not null) 
        { 
            await ApplyPendingActionAsync(); 
            if (_modalPlayer.Money < 0) { await RegisterLoserAsync(_modalPlayer); CleanupModal(); ResetPendingSpecial(); return; } 
            await BotAutoActionsIfNeeded(); 
            _game.NextTurn(); HasRolledThisTurn = false; 
            EnqueueGroup("transicao_turno", new DialogueContext { Player = _game.Players[_game.CurrentPlayerIndex].Name }, true); 
            await GameRepo.SaveGameAsync(GameId, _game); 
        } 
        CleanupModal(); 
        ResetPendingSpecial();
        StateHasChanged(); AnnounceHumanTurnIfNeeded(); AdvanceDialogueIfIdle(); await TryAutoRollForBotAsync(); }
    private void CleanupModal() { _modalFromMove = false; _modalTemplateEntity = null; }
    private void ResetPendingSpecial() { _pendingActionKind = PendingActionKind.None; _pendingAmount = 0; _pendingBackSteps = 0; }

    private void ConfigureTax(Block landed, Player player) { var percents = new[] { 5, 10, 15, 20, 25, 30 }; var pct = percents[_rand.Next(percents.Length)]; var taxAmount = (int)Math.Round(player.Money * pct / 100.0); _pendingActionKind = PendingActionKind.Tax; _pendingAmount = taxAmount; landed.Rent = taxAmount; }
    private void ConfigureChance() { var choices = new[] { 50, 100, 150, 200, 300, 350, 400, 500, 600, 700 }; _pendingActionKind = PendingActionKind.Chance; _pendingAmount = choices[_rand.Next(choices.Length)]; }
    private void ConfigureReves() { var takeMoneyOptions = new[] { 100, 200 }; _pendingActionKind = PendingActionKind.Reves; if (_rand.NextDouble() < 0.5) { _pendingAmount = takeMoneyOptions[_rand.Next(takeMoneyOptions.Length)]; } else { _pendingBackSteps = _rand.Next(2, 7); } }
    private async Task ApplyPendingActionAsync()
    { if (_game is null || _modalPlayer is null || _pendingActionKind == PendingActionKind.None) return; var player = _modalPlayer; var landed = _game.Board.FirstOrDefault(b => b.Position == player.CurrentPosition); switch (_pendingActionKind) { case PendingActionKind.Tax: player.Money = Math.Max(0, player.Money - _pendingAmount); if (landed is not null) landed.Rent = _pendingAmount; EnqueueGroup("evento_tax", new DialogueContext { Player = player.Name, Amount = _pendingAmount }, true, immediate: true); break; case PendingActionKind.Chance: player.Money += _pendingAmount; EnqueueGroup("evento_chance", new DialogueContext { Player = player.Name, Amount = _pendingAmount }, true, immediate: true); break; case PendingActionKind.Reves: if (_pendingBackSteps > 0) { await AnimateBackwardAsync(GetPlayerIndex(player.Id), _pendingBackSteps); EnqueueGroup("evento_reves", new DialogueContext { Player = player.Name, Steps = _pendingBackSteps }, true, immediate: true); } else if (_pendingAmount > 0) { player.Money = Math.Max(0, player.Money - _pendingAmount); EnqueueGroup("evento_reves", new DialogueContext { Player = player.Name, Amount = _pendingAmount }, true, immediate: true); } break; } await GameRepo.SaveGameAsync(GameId, _game); ResetPendingSpecial(); AdvanceDialogueIfIdle(); }

    private async Task OnBuyPropertyAsync() { if (_modalBlock is null || _modalPlayer is null) return; if (_game!.TryBuyProperty(_modalPlayer, _modalBlock)) { await GameRepo.SaveGameAsync(GameId, _game); EnqueueGroup("acao_compra", new DialogueContext { Player = _modalPlayer.Name, Block = _modalBlock.Name }, true, immediate: true); SyncOwnersToBoardSpaces(); } StateHasChanged(); }
    private async Task OnUpgradeAsync() { if (_modalBlock is PropertyBlock pb && _modalPlayer is not null && CanUpgradeAllowed(pb)) { if (pb.Upgrade(_modalPlayer)) { if (pb.BuildingType != BuildingType.None && pb.BuildingLevel > 0) { var evo = BuildingEvolutionDescriptions.Get(pb.BuildingType, Math.Clamp(pb.BuildingLevel,1,4)); pb.Name = evo.Name; } await GameRepo.SaveGameAsync(GameId, _game); SyncOwnersToBoardSpaces(); StateHasChanged(); EnqueueGroup("acao_upgrade", new DialogueContext { Player = _modalPlayer.Name, Block = pb.Name, Amount = pb.BuildingLevel }, true, immediate: true); } StateHasChanged(); } }
    private async Task OnSellPropertyAsync() { if (_modalBlock is PropertyBlock pb && pb.Owner is not null) { var owner = pb.Owner; owner.Money += pb.Price / 2; owner.OwnedProperties.Remove(pb); pb.Owner = null; pb.IsMortgaged = false; await GameRepo.SaveGameAsync(GameId, _game!); EnqueueGroup("acao_venda", new DialogueContext { Player = owner.Name, Block = pb.Name }, true, immediate: true); StateHasChanged(); } }
    private bool CanUpgradeAllowed(PropertyBlock pb) { if (_game is null || _modalPlayer is null) return false; if (pb.Owner != _modalPlayer) return false; if (_modalPlayer.CurrentPosition != pb.Position) return false; if (pb.BuildingType == BuildingType.None) return false; if (!pb.CanUpgrade()) return false; var nextCost = pb.BuildingPrices[pb.BuildingLevel]; if (_modalPlayer.Money < nextCost) return false; return true; }
    private int GetNextUpgradeCost(PropertyBlock pb) => pb.CanUpgrade() ? pb.BuildingPrices[pb.BuildingLevel] : 0;

    private async Task RegisterLoserAsync(Player player)
    { if (_game is null) return; player.IsBankrupt = true; foreach (var prop in player.OwnedProperties) { prop.Owner = null; prop.IsMortgaged = false; } player.OwnedProperties.Clear(); _loserName = player.Name; var ativos = _game.Players.Count(p => !p.IsBankrupt); if (ativos == 1) { var winner = _game.Players.First(p => !p.IsBankrupt); _winnerName = winner.Name; _showWinnerModal = true; _showLoserModal = false; _game.Finish(); EnqueueGroup("vitoria", new DialogueContext { Player = winner.Name }, true); } else { _showLoserModal = true; _showWinnerModal = false; _game.NextTurn(); HasRolledThisTurn = false; EnqueueGroup("eliminacao", new DialogueContext { Player = player.Name }, true); } await GameRepo.SaveGameAsync(GameId, _game); StateHasChanged(); AdvanceDialogueIfIdle(); }
    private void CloseLoserModal() { _showLoserModal = false; StateHasChanged(); _ = TryAutoRollForBotAsync(); }
    private void CloseWinnerModal() { _showWinnerModal = false; StateHasChanged(); }

    private async Task ShowDiceAnimationAsync(int finalDie1, int finalDie2)
    { try { var gifIndex = _rand.Next(1, 13); _rollingGifUrl = $"{Navigation.BaseUri}images/diceAnim/dice-rolling-{gifIndex}.gif"; _showDiceOverlay = true; StateHasChanged(); var totalMs = _rand.Next(2000, 3001); var frameMs = 80; var elapsed = 0; while (elapsed < totalMs) { _diceFace1 = _rand.Next(1, 7); _diceFace2 = _rand.Next(1, 7); StateHasChanged(); var delay = Math.Min(frameMs, totalMs - elapsed); try { await Task.Delay(delay); } catch { break; } elapsed += delay; } _diceFace1 = Math.Clamp(finalDie1, 1, 6); _diceFace2 = Math.Clamp(finalDie2, 1, 6); StateHasChanged(); } finally { _showDiceOverlay = false; _rollingGifUrl = string.Empty; StateHasChanged(); } }

    private int GetTrackLength() => Math.Min(Perimeter.Count, _game?.Board.Count ?? int.MaxValue);
    private async Task AnimateForwardAsync(int steps)
    { if (_game is null) return; var currentPlayer = _game.Players[_game.CurrentPlayerIndex]; var pos = currentPlayer.CurrentPosition; var track = GetTrackLength(); if (track <= 0) return; for (int i = 0; i < steps; i++) { pos = (pos + 1) % track; _pawnAnimPosition = pos; StateHasChanged(); try { await Task.Delay(_animStepMs); } catch { } } }
    private async Task AnimateBackwardAsync(int playerIndex, int steps)
    { if (_game is null || playerIndex < 0 || playerIndex >= _game.Players.Count) return; var prev = _isAnimating; _isAnimating = true; var player = _game.Players[playerIndex]; var pos = player.CurrentPosition; var track = GetTrackLength(); if (track <= 0) { _isAnimating = prev; return; } for (int i = 0; i < steps; i++) { pos = (pos - 1 + track) % track; _pawnAnimPosition = pos; StateHasChanged(); try { await Task.Delay(_animStepMs); } catch { } } player.CurrentPosition = pos; _pawnAnimPosition = -1; _isAnimating = prev; }

    private async Task HandleClick(BoardSpaceDto space)
    { if (_game is null || space is null || _showBlockModal) return; var parts = (space.Id ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); if (parts.Length < 2) return; if (!int.TryParse(parts[1], out var pos)) return; var block = _game.Board.FirstOrDefault(b => b.Position == pos); if (block is null) return; _modalFromMove = false; _modalBlock = block; _modalPlayer = _game.Players.ElementAtOrDefault(_game.CurrentPlayerIndex); _preMovePlayerMoney = _modalPlayer?.Money ?? 0; _modalTemplateEntity = _templatesByPosition.TryGetValue(pos, out var tmpl) ? tmpl : null; _showBlockModal = true; AddDialogueTemplate("{PLAYER} abriu {BLOCK}.", new DialogueContext { Player = _modalPlayer?.Name, Block = _modalBlock?.Name }, immediate: true); StateHasChanged(); await Task.CompletedTask; }

    private int GetHumanPlayersCount() { if (HumanCountQuery.HasValue) return Math.Max(0, HumanCountQuery.Value); try { var uri = Navigation.ToAbsoluteUri(Navigation.Uri); var q = QueryHelpers.ParseQuery(uri.Query); if (q.TryGetValue("humanCount", out var hv) && int.TryParse(hv.ToString(), out var parsed)) return Math.Max(0, parsed); } catch { } return 1; }
    private int GetPlayerIndex(Guid playerId) { if (_game is null) return -1; for (int i = 0; i < _game.Players.Count; i++) if (_game.Players[i].Id == playerId) return i; return -1; }
    private static List<(int r, int c)> BuildPerimeterClockwise(int rows, int cols) { var list = new List<(int r, int c)>(Math.Max(0, 2 * rows + 2 * cols - 4)); if (rows < 2 || cols < 2) return list; int bottom = rows - 1, top = 0, left = 0, right = cols - 1; for (int c = right; c >= left; c--) list.Add((bottom, c)); for (int r = bottom - 1; r >= top; r--) list.Add((r, left)); for (int c = left + 1; c <= right; c++) list.Add((top, c)); for (int r = top + 1; r <= bottom - 1; r++) list.Add((r, right)); return list; }
    private static string GetImageForType(BlockType? type) => type switch { BlockType.Go => "/images/blocks/property_basic.svg", BlockType.Property => "/images/blocks/property_basic.svg", BlockType.Company => "/images/blocks/property_predio.svg", BlockType.Jail => "/images/blocks/visitar_prisao.svg", BlockType.GoToJail => "/images/blocks/go_to_jail.svg", BlockType.Tax => "/images/blocks/volte-casas.svg", BlockType.Chance => "/images/blocks/sorte.png", BlockType.Reves => "/images/blocks/reves.png", _ => "/images/blocks/property_basic.svg" };
    private Func<int, string> _pawnUrlResolver => i => i < _pawnsForPlayers.Count ? $"{Navigation.BaseUri}images/pawns/PawnsB{_pawnsForPlayers[i]}.png" : PawnUrl;
    private void ParsePawnsQuery() { _pawnsForPlayers.Clear(); if (string.IsNullOrWhiteSpace(pawns)) return; foreach (var p in pawns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) if (int.TryParse(p, out var v)) _pawnsForPlayers.Add(Math.Clamp(v, 1, 6)); }

    private void AnnounceHumanTurnIfNeeded() { if (_game is null) return; if (!_initialIntroDone) return; if (IsCurrentPlayerHuman()) { var playerName = _game.Players[_game.CurrentPlayerIndex].Name; var line = _humanTurnTaunts[_rand.Next(_humanTurnTaunts.Length)]; AddDialogueTemplate(line, new DialogueContext { Player = playerName }); } }

    private class DialogueData { public Dictionary<string, List<DialogueLine>> Groups { get; set; } = new(); }
    private class DialogueLine { public string Id { get; set; } = string.Empty; public string Text { get; set; } = string.Empty; }
    private class DialogueContext { public string? Player { get; set; } public string? Block { get; set; } public int? Amount { get; set; } public int? Steps { get; set; } public int? Days { get; set; } }

    public async ValueTask DisposeAsync() { try { _typingCts?.Cancel(); } catch { } await Task.CompletedTask; }
}
