using System.ComponentModel.DataAnnotations;

namespace UFF.Monopoly.Data.Entities;

public class GameStateEntity
{
    [Key]
    public Guid Id { get; set; }
    public int CurrentPlayerIndex { get; set; }
    public bool IsFinished { get; set; }

    public ICollection<PlayerStateEntity> Players { get; set; } = new List<PlayerStateEntity>();
    public ICollection<BlockStateEntity> Board { get; set; } = new List<BlockStateEntity>();
}
