using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using UFF.Monopoly.Entities;
using UFF.Monopoly.Constants;

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase, IAsyncDisposable
{
    private Game? _game; private bool _loading = true;
    private List<Models.BoardSpaceDto> BoardSpaces { get; set; } = new();
    private Dictionary<int, Data.Entities.BlockTemplateEntity> _templatesByPosition = new();
    private int Rows; private int Cols; private int CellSize; private List<(int r, int c)> Perimeter = new();
    private double _boardScale = 1.0; private string _boardWidthCss = "0px"; private string _boardHeightCss = "0px";
    private string PawnUrl = "/images/pawns/PawnsB1.png"; private List<int> _pawnsForPlayers = new(); private int _pawnAnimPosition = -1;
    private string _centerCharStyle = string.Empty; private string _playersHudStyle = string.Empty; private string _turnActionsStyle = string.Empty; private string? _boardCenterImageUrl; private readonly string[] _ownerColors = PlayerColors.Colors;
    private readonly string[] _humanTurnTaunts = new[] { "Sua vez, {PLAYER}! Vamos ver se você consegue algo além de pagar aluguel.", "Sua vez, {PLAYER}. Capricha no dado... eu adoro quando você erra!", "É agora, {PLAYER}. Mostra serviço ou deixa que eu ensino como se joga.", "Sua vez! Se prepara pra tomar renda, {PLAYER}.", "Sua vez, {PLAYER}. Prometo pegar leve... mentira." , "Vai lá, {PLAYER}. Quanto mais você anda, mais você me deve." };
    private string _speedBtnStyle = string.Empty;
    private bool _hudLocked; // bloqueia HUD após primeira exibição

    protected override async Task OnParametersSetAsync() => await InitializeAsync();

    private async Task InitializeAsync()
    {
        _loading = true; HasRolledThisTurn = false;
        if (!boardId.HasValue) { Navigation.NavigateTo("/local"); return; }
        await LoadBoardLayoutAsync(boardId.Value);
        await LoadDialogueJsonAsync();
        _game = await GameRepo.GetGameAsync(GameId);
        // Reinício automático se jogo salvo já tem alguém com saldo 0 ou negativo / falido
        if (_game is not null && _game.Players.Any(p => p.Money <= 0 || p.IsBankrupt))
        {
            var restarted = await TryAutoRestartAsync();
            if (restarted) return; // navegação para novo jogo; interrompe init atual
        }
        if (_game is not null)
        { _pawnAnimPosition = _game.Players.FirstOrDefault()?.CurrentPosition ?? 0; _pawnsForPlayers = _game.Players.Select(p => Math.Clamp(p.PawnIndex, 1, 6)).ToList(); }
        if (!_pawnsForPlayers.Any()) ParsePawnsQuery();
        try { var pawn = await Profiles.GetPawnFromSessionAsync(); if (!string.IsNullOrWhiteSpace(pawn)) PawnUrl = pawn; } catch { }
        if (_game is not null) { SyncOwnersToBoardSpaces(); }
        if (!_dialogueInitialized)
        {
            var firstPlayerName = _game?.Players.FirstOrDefault()?.Name ?? "Jogador";
            EnqueueGroup("inicio", new DialogueContext { Player = firstPlayerName });
            AddDialogue("Eu sou o Sr. Monopoly. Só começam depois das minhas provocações. Mostrem que não vão falir tão rápido!");
            _dialogueInitialized = true; _initialIntroDone = false; _hudLocked = false;
        }
        AnnounceHumanTurnIfNeeded();
        AdvanceDialogueIfIdle();
        _loading = false; StateHasChanged();
        _ = TryAutoRollForBotAsync();
    }

    private async Task<bool> TryAutoRestartAsync()
    {
        if (_game is null) return false;
        if (!boardId.HasValue) return false;
        // Manter mesma lista de nomes e pawns ao reiniciar
        var names = _game.Players.Select(p => p.Name).ToList();
        var pawnIndices = _game.Players.Select(p => p.PawnIndex).ToList();
        var (newGameId, newGame) = await GameRepo.CreateNewGameAsync(boardId.Value, names, pawnIndices);
        // Monta query de pawns para manter seleção visual
        var pawnsQuery = string.Join(',', pawnIndices);
        var humanCount = GetHumanPlayersCount();
        Navigation.NavigateTo($"/play/{newGameId}?boardId={boardId.Value}&humanCount={humanCount}&pawns={pawnsQuery}");
        return true;
    }

    private void SyncOwnersToBoardSpaces()
    {
        if (_game is null) return;
        foreach (var space in BoardSpaces)
        {
            var parts = (space.Id ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || !int.TryParse(parts[1], out var pos)) continue;
            var block = _game.Board.FirstOrDefault(b => b.Position == pos);
            if (block?.Owner is null) { space.OwnerPlayerIndex = null; }
            else { var idx = GetPlayerIndex(block.Owner.Id); space.OwnerPlayerIndex = idx >= 0 ? idx : null; }
            if (block is PropertyBlock pb)
            {
                if (pb.BuildingType != BuildingType.None && pb.BuildingLevel > 0)
                { var evo = Components.Pages.BoardBuilders.BuildingEvolutionDescriptions.Get(pb.BuildingType, Math.Clamp(pb.BuildingLevel, 1, 4)); pb.Name = evo.Name; }
                space.Name = pb.Name; space.BuildingType = pb.BuildingType; space.BuildingLevel = pb.BuildingLevel; space.ImageUrl = string.IsNullOrWhiteSpace(pb.ImageUrl) ? space.ImageUrl : pb.ImageUrl;
            }
        }
    }

    private async Task HandleClick(Models.BoardSpaceDto space)
    { if (_game is null || space is null || _showBlockModal) return; var parts = (space.Id ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); if (parts.Length < 2) return; if (!int.TryParse(parts[1], out var pos)) return; var block = _game.Board.FirstOrDefault(b => b.Position == pos); if (block is null) return; _modalFromMove = false; _modalBlock = block; _modalPlayer = _game.Players.ElementAtOrDefault(_game.CurrentPlayerIndex); _preMovePlayerMoney = _modalPlayer?.Money ?? 0; _modalTemplateEntity = _templatesByPosition.TryGetValue(pos, out var tmpl) ? tmpl : null; _showBlockModal = true; AddDialogueTemplate("{PLAYER} abriu {BLOCK}.", new DialogueContext { Player = _modalPlayer?.Name, Block = _modalBlock?.Name }, immediate: true); StateHasChanged(); await Task.CompletedTask; }

    private int GetHumanPlayersCount()
    { if (HumanCountQuery.HasValue) return Math.Max(0, HumanCountQuery.Value); try { var uri = Navigation.ToAbsoluteUri(Navigation.Uri); var q = QueryHelpers.ParseQuery(uri.Query); if (q.TryGetValue("humanCount", out var hv) && int.TryParse(hv.ToString(), out var parsed)) return Math.Max(0, parsed); } catch { } return 1; }

    private int GetPlayerIndex(Guid playerId)
    { if (_game is null) return -1; for (int i = 0; i < _game.Players.Count; i++) if (_game.Players[i].Id == playerId) return i; return -1; }

    private int GetTrackLength() => Math.Min(Perimeter.Count, _game?.Board.Count ?? int.MaxValue);

    private Func<int, string> _pawnUrlResolver => i => i < _pawnsForPlayers.Count ? $"{Navigation.BaseUri}images/pawns/PawnsB{_pawnsForPlayers[i]}.png" : PawnUrl;

    private void ParsePawnsQuery()
    { _pawnsForPlayers.Clear(); if (string.IsNullOrWhiteSpace(pawns)) return; foreach (var p in pawns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) if (int.TryParse(p, out var v)) _pawnsForPlayers.Add(Math.Clamp(v, 1, 6)); }

    public async ValueTask DisposeAsync() { try { _typingCts?.Cancel(); } catch { } try { _chatPauseCts?.Cancel(); } catch { } await Task.CompletedTask; }
}
