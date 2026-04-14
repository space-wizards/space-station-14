using System;
using Content.MapEditor.Commands;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Click to set center, drag to set radius, release to fill a circle of tiles.
///     Uses bounding-box iteration with distance check for simplicity.
/// </summary>
public sealed class CircleTool : IEditorTool
{
    public string Name => "Circle";

    /// <summary>Center of the circle (mouse down position), exposed for overlay preview.</summary>
    public Vector2i? DragStart { get; private set; }

    /// <summary>Current edge point of the circle, exposed for overlay preview.</summary>
    public Vector2i? DragEnd { get; private set; }

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        DragStart = tilePos;
        DragEnd = tilePos;
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos)
    {
        DragEnd = tilePos;
    }

    public void OnMouseUp(ToolContext ctx)
    {
        if (DragStart == null || DragEnd == null)
            return;

        var center = DragStart.Value;
        var end = DragEnd.Value;

        var dx = end.X - center.X;
        var dy = end.Y - center.Y;
        var radiusSq = dx * dx + dy * dy;

        // If radius is 0, just paint the single center tile.
        var radius = (int) Math.Ceiling(Math.Sqrt(radiusSq));

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var batch = new BatchCommand();

        for (var x = center.X - radius; x <= center.X + radius; x++)
        {
            for (var y = center.Y - radius; y <= center.Y + radius; y++)
            {
                var distSq = (x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y);
                if (distSq > radiusSq)
                    continue;

                var pos = new Vector2i(x, y);
                var oldTile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

                if (oldTile == ctx.SelectedTile)
                    continue;

                var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, oldTile, ctx.SelectedTile);
                cmd.Execute();
                batch.Add(cmd);
            }
        }

        if (batch.Count > 0)
            ctx.CommandStack.Push(batch);

        DragStart = null;
        DragEnd = null;
    }
}
