using System.Collections.Generic;
using Content.MapEditor.Commands;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Draws cables (HV, MV, APC) along a drag stroke on the active grid.
///     Each tile is placed at most once per stroke, and the full stroke is batched
///     into a single undo unit.
/// </summary>
public sealed class CableDrawTool : IEditorTool
{
    public string Name => "Cable Draw";

    private BatchCommand? _batch;
    private readonly HashSet<Vector2i> _placedThisStroke = new();

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        _batch = new BatchCommand();
        _placedThisStroke.Clear();
        PlaceCable(ctx, tilePos);
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos)
    {
        PlaceCable(ctx, tilePos);
    }

    public void OnMouseUp(ToolContext ctx)
    {
        if (_batch != null && _batch.Count > 0)
            ctx.CommandStack.Push(_batch);

        _batch = null;
        _placedThisStroke.Clear();
    }

    private void PlaceCable(ToolContext ctx, Vector2i tilePos)
    {
        if (_batch == null)
            return;

        if (!_placedThisStroke.Add(tilePos))
            return;

        if (string.IsNullOrEmpty(ctx.SelectedCablePrototype))
            return;

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);

        // GridTileToLocal returns tile center (adds TileSizeHalfVector).
        var coords = ctx.MapSystem.GridTileToLocal(gridUid, grid, tilePos);

        var uid = ctx.EntityManager.SpawnEntity(ctx.SelectedCablePrototype, coords);

        var cmd = new SpawnEntityCommand(ctx.EntityManager, uid);
        cmd.Execute(); // Already spawned, but satisfy the command pattern.
        _batch.Add(cmd);
    }
}
