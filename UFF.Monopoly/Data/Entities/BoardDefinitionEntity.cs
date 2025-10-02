using System.ComponentModel.DataAnnotations;

namespace UFF.Monopoly.Data.Entities;

public class BoardDefinitionEntity
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BlockTemplateEntity> Blocks { get; set; } = new List<BlockTemplateEntity>();
}
