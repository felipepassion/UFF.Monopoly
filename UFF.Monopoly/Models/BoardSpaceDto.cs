namespace UFF.Monopoly.Models;

public class BoardSpaceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public BoardSpaceStyle Style { get; set; } = new();
}

public class BoardSpaceStyle
{
    public string Top { get; set; } = "0px";
    public string Left { get; set; } = "0px";
    public string Width { get; set; } = "0px";
    public string Height { get; set; } = "0px";
    public string? Transform { get; set; }
}
