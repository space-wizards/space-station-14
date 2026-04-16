using System;
using Content.MapEditor.Commands;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Click-and-drag to fill a rectangular area with the selected tile.
///     Start corner is recorded on mouse down, end corner on mouse up.
/// </summary>
public sealed class RectangleTool : IEditorTool
{
    public string Name => "Rectangle";

    /// <summary>Start corner of the drag, exposed for overlay preview.</summary>
    public Vector2i? DragStart { get; private set; }

    /// <summary>Current end corner of the drag, exposed for overlay preview.</summary>
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

        var start = DragStart.Value;
        var end = DragEnd.Value;

        var minX = Math.Min(start.X, end.X);
        var maxX = Math.Max(start.X, end.X);
        var minY = Math.Min(start.Y, end.Y);
        var maxY = Math.Max(start.Y, end.Y);

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var batch = new BatchCommand();

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var pos = new Vector2i(x, y);
                var oldTile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

                if (oldTile.TypeId == ctx.SelectedTile.TypeId)
                    continue;

                var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, oldTile, ctx.GetVariantTile());
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
