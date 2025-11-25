using System.ComponentModel.DataAnnotations;

namespace UFF.Monopoly.Data.Entities;

public class BoardDefinitionEntity
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Persisted board layout
    public int Rows { get; set; } = 5;
    public int Cols { get; set; } = 5;
    public int CellSizePx { get; set; } = 64;

    // Image rendered in the board center (inner area)
    public string? CenterImageUrl { get; set; }

    public ICollection<BlockTemplateEntity> Blocks { get; set; } = new List<BlockTemplateEntity>();
}

