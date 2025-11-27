using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using UFF.Monopoly.Data;
using UFF.Monopoly.Data.Entities;
using UFF.Monopoly.Entities;
using UFF.Monopoly.Models;
using UFF.Monopoly.Components.Pages.BoardBuilders;

namespace UFF.Monopoly.Components.Pages.GamePlay;

public partial class Play : ComponentBase, IAsyncDisposable
{
    private async Task LoadBoardLayoutAsync(Guid bId)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var board = await db.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bId);
        if (board is null) return;
        Rows = board.Rows; Cols = board.Cols; CellSize = board.CellSizePx; Perimeter = BuildPerimeterClockwise(Rows, Cols);
        _boardScale = CellSize switch { < 50 => 1.5, < 70 => 1.3, < 90 => 1.15, _ => 1.0 };
        _boardCenterImageUrl = board.CenterImageUrl;
        var templates = await db.BlockTemplates.AsNoTracking().Where(t => t.BoardDefinitionId == bId).OrderBy(t => t.Position).ToListAsync();
        if (templates.Count > 0 && Perimeter.Count != templates.Count)
        { var effective = Math.Min(Perimeter.Count, templates.Count); Perimeter = Perimeter.Take(effective).ToList(); }
        _templatesByPosition = templates.ToDictionary(t => t.Position, t => t);
        BoardSpaces = new List<BoardSpaceDto>(Perimeter.Count);
        for (int i = 0; i < Perimeter.Count; i++)
        {
            var (r, c) = Perimeter[i]; var template = templates.ElementAtOrDefault(i);
            var img = template != null && !string.IsNullOrWhiteSpace(template.ImageUrl) ? template.ImageUrl : GetImageForType(template?.Type ?? BlockType.Property);
            BoardSpaces.Add(new BoardSpaceDto
            {
                Id = $"space-{i}", Name = template?.Name ?? $"Space {i}", ImageUrl = img, Price = template?.Price ?? 0, Rent = template?.Rent ?? 0,
                Type = template?.Type ?? BlockType.Property, BuildingType = template?.BuildingType ?? BuildingType.None, BuildingLevel = template?.Level is null ? 0 : (int)template.Level,
                Style = new BoardSpaceStyle { Top = $"{(int)(r * CellSize * _boardScale)}px", Left = $"{(int)(c * CellSize * _boardScale)}px", Width = $"{(int)(CellSize * _boardScale)}px", Height = $"{(int)(CellSize * _boardScale)}px" }
            });
        }
        _boardWidthCss = ((int)(Cols * CellSize * _boardScale)) + "px"; _boardHeightCss = ((int)(Rows * CellSize * _boardScale)) + "px";
        var cellScaled = (int)(CellSize * _boardScale);
        var imgHeight = (int)(cellScaled * 1.7);
        var bottomRowTop = (int)((Rows - 1) * cellScaled);
        var top = bottomRowTop - imgHeight - (int)(cellScaled * 0.3); if (top < 0) top = 0;
        var estimatedWidth = (int)(imgHeight * 0.75);
        var rightColLeft = (Cols - 1) * cellScaled; var marginChar = (int)(cellScaled * 0.15);
        var left = rightColLeft - estimatedWidth - marginChar; if (left < marginChar) left = marginChar;
        _centerCharStyle = $"position:absolute;top:{top}px;left:{left}px;height:{imgHeight}px;z-index:1500;pointer-events:none;filter:drop-shadow(0 12px 24px rgba(0,0,0,0.6));";
        var btnSize = Math.Max(24, (int)(cellScaled * 0.4));
        var btnTop = (top + imgHeight - btnSize) - 15;
        var btnLeft = left + estimatedWidth - (int)(cellScaled * 1.8);
        _speedBtnStyle = $"position:absolute;top:{btnTop}px;left:{btnLeft}px;width:{btnSize}px;height:{btnSize}px;z-index:1550;pointer-events:auto;display:flex;align-items:center;justify-content:center;background:#1f1f1fbb;color:#fff;border:1px solid #4ad2a0;border-radius:8px;box-shadow:0 6px 12px rgba(0,0,0,0.5);font-weight:700;";
        if (Rows >= 3 && Cols >= 3)
        {
            var interiorLeft = cellScaled; var interiorTop = cellScaled; var interiorWidth = (Cols - 2) * cellScaled; var interiorHeight = (Rows - 2) * cellScaled; var margin = (int)(cellScaled * 0.2);
            var chatHeight = Math.Min((int)(cellScaled * 1.6), Math.Max(cellScaled, (int)(interiorHeight * 0.42)));
            var chatTop = interiorTop + interiorHeight - chatHeight - margin;
            _chatContainerStyle = $"position:absolute;left:{interiorLeft}px;top:{chatTop}px;width:{interiorWidth}px;height:{chatHeight}px;background:url('/images/mr_monopoly/conversation-container.png') center/100% 100% no-repeat;z-index:1200;pointer-events:none;";
            var textPadLeft = (int)(cellScaled * 0.25); var textPadRight = (int)(cellScaled * 1.8); var textPadTop = (int)(cellScaled * 0.23); var textPadBottom = (int)(cellScaled * 0.2);
            var fontSize = Math.Max(9, (int)(cellScaled * 0.16));
            _chatTextStyle = $"position:absolute;left:{textPadLeft}px;top:{textPadTop}px;right:{textPadRight}px;bottom:{textPadBottom}px;font-size:{fontSize}px;line-height:1.15;color:#d4e9dc;font-family:'Segoe UI',sans-serif;font-weight:600;text-shadow:0 2px 4px rgba(0,0,0,0.7);overflow:hidden;display:flex;align-items:flex-start;word-wrap:break-word;";
            var hudWidth = Math.Min(interiorWidth, (int)(cellScaled * 14));
            var hudLeft = interiorLeft + (interiorWidth - hudWidth) / 2;
            var hudBottomGap = (int)(cellScaled * 0.15);
            var hudHeight = (int)(cellScaled * 2.1);
            var hudTop = chatTop - hudHeight - hudBottomGap; if (hudTop < interiorTop) hudTop = interiorTop + margin;
            _playersHudStyle = $"left:{hudLeft}px;top:{hudTop}px;width:{hudWidth}px;z-index:1600;";
            var actionsWidth = Math.Min((int)(cellScaled * 6.2), interiorWidth);
            var actionsLeft = interiorLeft + (interiorWidth - actionsWidth) / 2;
            var actionsTop = hudTop + hudHeight + (int)(cellScaled * 0.05);
            _turnActionsStyle = $"position:absolute;left:{actionsLeft}px;top:{actionsTop}px;width:{actionsWidth}px;display:flex;justify-content:center;gap:12px;z-index:1650;";
        }
        else
        {
            var chatHeightSmall = (int)(cellScaled * 2.0);
            _chatContainerStyle = $"position:absolute;left:0;bottom:0;width:{Cols * cellScaled}px;height:{chatHeightSmall}px;background:url('/images/mr_monopoly/conversation-container.png') center/100% 100% no-repeat;z-index:1200;pointer-events:none;";
            _chatTextStyle = $"position:absolute;left:{(int)(cellScaled * 0.2)}px;top:{(int)(cellScaled * 0.22)}px;right:{(int)(cellScaled * 1.8)}px;bottom:{(int)(cellScaled * 0.3)}px;font-size:{Math.Max(8, (int)(cellScaled * 0.28))}px;line-height:1.15;color:#d4e9dc;font-family:'Segoe UI',sans-serif;font-weight:600;text-shadow:0 2px 4px rgba(0,0,0,0.7);overflow:hidden;display:flex;align-items:flex-start;word-wrap:break-word;";
            _playersHudStyle = $"left:0;top:0;width:{Cols * cellScaled}px;z-index:1600;";
            _turnActionsStyle = $"position:absolute;left:0;top:{(int)(cellScaled * 2.3)}px;width:{Cols * cellScaled}px;display:flex;justify-content:center;gap:12px;z-index:1650;";
        }
    }

    private static List<(int r, int c)> BuildPerimeterClockwise(int rows, int cols)
    {
        var list = new List<(int r, int c)>(Math.Max(0, 2 * rows + 2 * cols - 4)); if (rows < 2 || cols < 2) return list;
        int bottom = rows - 1, top = 0, left = 0, right = cols - 1;
        for (int c = right; c >= left; c--) list.Add((bottom, c));
        for (int r = bottom - 1; r >= top; r--) list.Add((r, left));
        for (int c = left + 1; c <= right; c++) list.Add((top, c));
        for (int r = top + 1; r <= bottom - 1; r++) list.Add((r, right));
        return list;
    }

    private static string GetImageForType(BlockType? type) => type switch { BlockType.Go => "/images/blocks/property_basic.svg", BlockType.Property => "/images/blocks/property_basic.svg", BlockType.Company => "/images/blocks/property_predio.svg", BlockType.Jail => "/images/blocks/visitar_prisao.svg", BlockType.GoToJail => "/images/blocks/go_to_jail.svg", BlockType.Tax => "/images/blocks/volte-casas.svg", BlockType.Chance => "/images/blocks/sorte.png", BlockType.Reves => "/images/blocks/reves.png", _ => "/images/blocks/property_basic.svg" };
}
