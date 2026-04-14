using System.Collections.Generic;
using Content.MapEditor.Commands;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Erases tiles by setting them to Tile.Empty on the active grid.
///     Same stroke logic as PaintTool but always paints empty.
/// </summary>
public sealed class EraseTool : IEditorTool
{
    public string Name => "Erase";

    private BatchCommand? _batch;
    private readonly HashSet<Vector2i> _erasedThisStroke = new();

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        _batch = new BatchCommand();
        _erasedThisStroke.Clear();
        EraseTile(ctx, tilePos);
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos)
    {
        EraseTile(ctx, tilePos);
    }

    public void OnMouseUp(ToolContext ctx)
    {
        if (_batch != null && _batch.Count > 0)
            ctx.CommandStack.Push(_batch);

        _batch = null;
        _erasedThisStroke.Clear();
    }

    private void EraseTile(ToolContext ctx, Vector2i pos)
    {
        if (_batch == null)
            return;

        if (!_erasedThisStroke.Add(pos))
            return;

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var oldTile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

        if (oldTile.IsEmpty)
            return; // Already empty.

        var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, oldTile, Tile.Empty);
        cmd.Execute();
        _batch.Add(cmd);
    }
}
