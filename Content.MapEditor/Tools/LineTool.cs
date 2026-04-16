using System;
using System.Collections.Generic;
using Content.MapEditor.Commands;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Click-and-drag to draw a line of tiles using Bresenham's algorithm.
/// </summary>
public sealed class LineTool : IEditorTool
{
    public string Name => "Line";

    /// <summary>Start point of the drag, exposed for overlay preview.</summary>
    public Vector2i? DragStart { get; private set; }

    /// <summary>Current end point of the drag, exposed for overlay preview.</summary>
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

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var batch = new BatchCommand();

        foreach (var pos in GetLinePoints(DragStart.Value, DragEnd.Value))
        {
            var oldTile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

            if (oldTile.TypeId == ctx.SelectedTile.TypeId)
                continue;

            var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, oldTile, ctx.GetVariantTile());
            cmd.Execute();
            batch.Add(cmd);
        }

        if (batch.Count > 0)
            ctx.CommandStack.Push(batch);

        DragStart = null;
        DragEnd = null;
    }

    /// <summary>
    ///     Bresenham's line algorithm. Yields all tile positions along the line from <paramref name="from"/> to <paramref name="to"/>.
    /// </summary>
    public static IEnumerable<Vector2i> GetLinePoints(Vector2i from, Vector2i to)
    {
        int dx = Math.Abs(to.X - from.X), sx = from.X < to.X ? 1 : -1;
        int dy = -Math.Abs(to.Y - from.Y), sy = from.Y < to.Y ? 1 : -1;
        int err = dx + dy;
        var x = from.X;
        var y = from.Y;

        while (true)
        {
            yield return new Vector2i(x, y);
            if (x == to.X && y == to.Y)
                break;

            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
}
