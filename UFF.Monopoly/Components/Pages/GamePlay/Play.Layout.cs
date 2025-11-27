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
    private record ViewportInfo(int width, int height);

    private async Task LoadBoardLayoutAsync(Guid bId)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var board = await db.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bId);
        if (board is null) return;
        Rows = board.Rows; Cols = board.Cols; CellSize = board.CellSizePx; Perimeter = BuildPerimeterClockwise(Rows, Cols);

        // Base scale defined by card size
        _boardScale = CellSize switch { < 50 => 1.5, < 70 => 1.3, < 90 => 1.15, _ => 1.0 };
        var cellScaled = (int)(CellSize * _boardScale);

        // Viewport and available space
        int vw = 0, vh = 0;
        try { var vp = await JSRuntime.InvokeAsync<ViewportInfo>("boardBuilder.getViewport", Array.Empty<object>()); vw = vp.width; vh = vp.height; } catch { }
        if (vw <= 0) vw = 1024; if (vh <= 0) vh = 768;
        var pad = 0.92; // keep some breathing room
        var availableWidth = (int)(vw * pad);
        var availableHeight = (int)(vh * pad);

        // Calculate dynamic gutters so the board fills available area without changing block size
        var baseBoardWidth = Cols * cellScaled;
        var baseBoardHeight = Rows * cellScaled;
        var extraW = Math.Max(0, availableWidth - baseBoardWidth);
        var extraH = Math.Max(0, availableHeight - baseBoardHeight);
        _gutterX = Cols > 1 ? (double)extraW / (Cols - 1) : 0.0;
        _gutterY = Rows > 1 ? (double)extraH / (Rows - 1) : 0.0;

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
            var topPx = (int)(r * (cellScaled + _gutterY));
            var leftPx = (int)(c * (cellScaled + _gutterX));
            BoardSpaces.Add(new BoardSpaceDto
            {
                Id = $"space-{i}", Name = template?.Name ?? $"Space {i}", ImageUrl = img, Price = template?.Price ?? 0, Rent = template?.Rent ?? 0,
                Type = template?.Type ?? BlockType.Property, BuildingType = template?.BuildingType ?? BuildingType.None, BuildingLevel = template?.Level is null ? 0 : (int)template.Level,
                Style = new BoardSpaceStyle { Top = $"{topPx}px", Left = $"{leftPx}px", Width = $"{cellScaled}px", Height = $"{cellScaled}px" }
            });
        }
        // Final board size fills available area
        _boardWidthCss = Math.Max(baseBoardWidth, availableWidth) + "px";
        _boardHeightCss = Math.Max(baseBoardHeight, availableHeight) + "px";

        // Derivative element positions based on scaled cell size and gutters
        var imgHeight = (int)(cellScaled * 1.7);
        var bottomRowTop = (int)((Rows - 1) * (cellScaled + _gutterY));
        var top = bottomRowTop - imgHeight - (int)(cellScaled * 0.3); if (top < 0) top = 0;
        var estimatedWidth = (int)(imgHeight * 0.75);
        var rightColLeft = (int)((Cols - 1) * (cellScaled + _gutterX)); var marginChar = (int)(cellScaled * 0.15);
        var left = rightColLeft - estimatedWidth - marginChar; if (left < marginChar) left = marginChar;
        _centerCharStyle = $"position:absolute;top:{top}px;left:{left}px;height:{imgHeight}px;z-index:1500;pointer-events:none;filter:drop-shadow(0 12px 24px rgba(0,0,0,0.6));";
        var btnSize = Math.Max(24, (int)(cellScaled * 0.4));
        var btnTop = (top + imgHeight - btnSize) - 15;
        var btnLeft = left + estimatedWidth - (int)(cellScaled * 1.8);
        _speedBtnStyle = $"position:absolute;top:{btnTop}px;left:{btnLeft}px;width:{btnSize}px;height:{btnSize}px;z-index:1550;pointer-events:auto;display:flex;align-items:center;justify-content:center;background:#1f1f1fbb;color:#fff;border:1px solid #4ad2a0;border-radius:8px;box-shadow:0 6px 12px rgba(0,0,0,0.5);font-weight:700;";

        // NEW CHAT SIZING LOGIC (clean, deterministic):
        // Define interior area (space not occupied by border blocks)
        var interiorLeft = (int)(cellScaled + _gutterX);
        var interiorTop = (int)(cellScaled + _gutterY);
        var interiorWidth = (int)((Cols - 2) * (cellScaled + _gutterX));
        var interiorHeight = (int)((Rows - 2) * (cellScaled + _gutterY));

        // Side offset equals lateral block total width, but reduce a bit so chat encosta na parede
        var sideOffsetBase = (int)(cellScaled + _gutterX);
        var reduce = (int)Math.Max(cellScaled * 0.75, _gutterX); // pull in towards the inner wall
        var sideOffset = Math.Max(0, sideOffsetBase - reduce);

        // Target a fixed ratio for chat height relative to interior height, clamp by cell size
        // Works for sparse boards (small Rows/Cols) and dense boards alike
        var minChatH = Math.Max(cellScaled, (int)(cellScaled * 1.2));
        var maxChatH = 200; // absolute max height requested
        var targetRatio = 0.42; // baseline proportion of interior height
        var rawChatH = (int)(interiorHeight * targetRatio);
        var chatHeight = Math.Clamp(rawChatH, minChatH, maxChatH);

        // Bottom margin inside interior so chat does not collide with bottom blocks
        var bottomMargin = (int)(cellScaled * 0.15);
        var chatTop = interiorTop + interiorHeight - chatHeight - bottomMargin;
        if (chatTop < interiorTop) chatTop = interiorTop; // guard when boards are very short

        _chatContainerStyle = $"position:absolute;left:{sideOffset}px;right:{sideOffset}px;top:{chatTop}px;height:{chatHeight}px;max-height:200px;background:url('/images/mr_monopoly/conversation-container.png') center/100% 100% no-repeat;z-index:1200;pointer-events:none;";

        // Chat text paddings and font size adjustments (slightly smaller font, a bit more top/left padding)
        var textPadLeft = (int)(cellScaled * 0.25) + 14; // +4 extra left padding
        var textPadRight = (int)(cellScaled * 1.8);
        var textPadTop = (int)(cellScaled * 0.23) + 24; // +4 extra top padding
        var textPadBottom = (int)(cellScaled * 0.2);
        var fontSize = Math.Max(10, (int)(cellScaled * 0.18)) + 12; // reduced a little from +15 to +12
        _chatTextStyle = $"position:absolute;left:{textPadLeft}px;top:{textPadTop}px;right:{textPadRight}px;bottom:{textPadBottom}px;font-size:{fontSize}px;line-height:1.18;color:#d4e9dc;font-family:'Segoe UI',sans-serif;font-weight:600;text-shadow:0 2px 4px rgba(0,0,0,0.7);overflow:hidden;display:flex;align-items:flex-start;word-wrap:break-word;";

        // HUD and actions position
        var hudWidth = Math.Min(interiorWidth, (int)(cellScaled * 14));
        var hudBottomGap = (int)(cellScaled * 0.15);
        var hudHeight = (int)(cellScaled * 2.1);
        var hudTop = chatTop - hudHeight - hudBottomGap; if (hudTop < interiorTop) hudTop = interiorTop + (int)(cellScaled * 0.2) - 50;
        _playersHudStyle = $"position:absolute;left:50%;top:25%;width:{hudWidth}px;transform:translateX(-50%);z-index:1600;";

        var actionsWidth = Math.Min((int)(cellScaled * 6.2), interiorWidth);
        var actionsTop = hudTop + hudHeight + (int)(cellScaled * 0.05);
        _turnActionsStyle = $"position:absolute;left:50%;top:{actionsTop}px;width:{actionsWidth}px;transform:translateX(-50%);display:flex;justify-content:center;gap:12px;z-index:1650;";
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

    private static string GetImageForType(BlockType? type) => type switch { BlockType.Go => "/images/blocks/property_basic.svg", BlockType.Property => "/images/blocks/property_basic.svg", BlockType.Company => "/images/blocks/property_predio.svg", BlockType.GoToJail => "/images/blocks/gotojail.png", BlockType.Tax => "/images/blocks/reves.png", BlockType.Chance => "/images/blocks/sorte.png", BlockType.Reves => "/images/blocks/reves.png", _ => "/images/blocks/property_basic.svg" };
}
