using System.Collections.Generic;
using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Erases tiles by setting them to Tile.Empty.
///     Same stroke logic as PaintTool but always paints empty.
/// </summary>
public sealed class EraseTool : IEditorTool
{
    public string Name => "Erase";

    private BatchCommand? _batch;
    private readonly HashSet<Vector2i> _erasedThisStroke = new();

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos, EntityUid gridUid)
    {
        _batch = new BatchCommand();
        _erasedThisStroke.Clear();
        EraseTile(ctx, tilePos, gridUid);
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos, EntityUid gridUid)
    {
        EraseTile(ctx, tilePos, gridUid);
    }

    public void OnMouseUp(ToolContext ctx)
    {
        if (_batch != null && _batch.Count > 0)
            ctx.CommandStack.Push(_batch);

        _batch = null;
        _erasedThisStroke.Clear();
    }

    private void EraseTile(ToolContext ctx, Vector2i pos, EntityUid gridUid)
    {
        if (_batch == null)
            return;

        if (!_erasedThisStroke.Add(pos))
            return;

        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var oldTile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

        if (oldTile.IsEmpty)
            return; // Already empty.

        var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, oldTile, Tile.Empty);
        cmd.Execute();
        _batch.Add(cmd);
    }
}
