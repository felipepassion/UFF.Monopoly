namespace UFF.Monopoly.Models;

using UFF.Monopoly.Entities;

public class BoardSpaceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public BoardSpaceStyle Style { get; set; } = new();
    public int Price { get; set; } // preço de compra
    public int Rent { get; set; } // aluguel base
    public BlockType Type { get; set; } = BlockType.Property;
    public BuildingType BuildingType { get; set; } = BuildingType.None;
    public int BuildingLevel { get; set; } = 0; // 0..4
    // Novo: índice do jogador dono (para overlay de cor). Null se sem dono.
    public int? OwnerPlayerIndex { get; set; }
}

public class BoardSpaceStyle
{
    public string Top { get; set; } = "0px";
    public string Left { get; set; } = "0px";
    public string Width { get; set; } = "0px";
    public string Height { get; set; } = "0px";
    public string? Transform { get; set; }
}
