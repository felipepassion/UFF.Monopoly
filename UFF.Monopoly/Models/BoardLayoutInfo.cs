using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Models;

namespace UFF.Monopoly.Models;

public sealed class BoardLayoutInfo
{
    public required List<(int r,int c)> Perimeter { get; init; }
    public required Dictionary<int, BlockTemplateEntity> TemplatesByPosition { get; init; }
    public required int Rows { get; init; }
    public required int Cols { get; init; }
    public required int CellSize { get; init; }
    public required string BoardWidthCss { get; init; }
    public required string BoardHeightCss { get; init; }
    public required List<BoardSpaceDto> Spaces { get; init; }
}
