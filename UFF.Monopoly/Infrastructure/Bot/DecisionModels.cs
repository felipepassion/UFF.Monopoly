using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Infrastructure.Bot;

/// <summary>
/// Contexto simplificado de decisão (fase 1).
/// </summary>
public class DecisionContext
{
    public Game? Game { get; init; }
    public Player? CurrentPlayer { get; init; }
    public Block? CurrentBlock { get; init; }
    public bool HasRolledThisTurn { get; init; }
    public bool IsTypingChat { get; init; }
    public bool IsAnimating { get; init; }
    public bool ModalFromMove { get; init; }
}

/// <summary>
/// Tipos de decisão iniciais.
/// </summary>
public enum DecisionType
{
    None,
    Roll,
    Buy,
    Upgrade,
    EndTurn,
    Skip
}

/// <summary>
/// Resultado de uma decisão (simplificado).
/// </summary>
public class DecisionResult
{
    public DecisionType Type { get; init; }
    public Block? TargetBlock { get; init; }
    public string? Reason { get; init; }
    public int Priority { get; init; }
    public int SuggestedDelayMs { get; init; }
    public bool IsCancelable { get; init; }

    public static DecisionResult Simple(DecisionType t, string? r = null, int prio = 0, int delay = 0, bool cancelable = true, Block? target = null)
        => new() { Type = t, Reason = r, Priority = prio, SuggestedDelayMs = delay, IsCancelable = cancelable, TargetBlock = target };
}
