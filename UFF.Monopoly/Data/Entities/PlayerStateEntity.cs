using System.ComponentModel.DataAnnotations;

namespace UFF.Monopoly.Data.Entities;

public class PlayerStateEntity
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CurrentPosition { get; set; }
    public int Money { get; set; }
    public bool InJail { get; set; }
    public int GetOutOfJailFreeCards { get; set; }
    public int JailTurns { get; set; }
    public bool IsBankrupt { get; set; }

    // pawn index selection (1..6)
    public int PawnIndex { get; set; } = 1;

    public Guid GameStateId { get; set; }
    public GameStateEntity Game { get; set; } = null!;
}
