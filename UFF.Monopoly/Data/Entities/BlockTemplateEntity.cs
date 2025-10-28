using System.ComponentModel.DataAnnotations;
using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Data.Entities;

public class BlockTemplateEntity
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
    public BlockType Type { get; set; }

    // Property-specific configuration
    public PropertyLevel? Level { get; set; }
    public int HousePrice { get; set; }
    public int HotelPrice { get; set; }
    // rents stored as CSV for template: values for 0..4 houses and 1..2 hotels (total 7 values)
    public string? RentsCsv { get; set; }

    // FK explícita para o board, necessária para compor índice único por board
    public Guid BoardDefinitionId { get; set; }
}
