using System.ComponentModel.DataAnnotations;
using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Data.Entities;

public class BlockStateEntity
{
    [Key]
    public Guid Id { get; set; }

    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Rent { get; set; }
    public Guid? OwnerId { get; set; }
    public bool IsMortgaged { get; set; }
    public BlockType Type { get; set; }

    // Persist dynamic property state
    public int Houses { get; set; }
    public int Hotels { get; set; }

    // Optional group id to identify property sets (monopolies)
    public Guid? GroupId { get; set; }

    public Guid GameStateId { get; set; }
    public GameStateEntity Game { get; set; } = null!;
}
