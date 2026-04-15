using System.Collections.Generic;
using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Draws pipes (atmos supply, disposal) along a drag stroke on the active grid.
///     Each tile is placed at most once per stroke, and the full stroke is batched
///     into a single undo unit.
/// </summary>
public sealed class PipeDrawTool : IEditorTool
{
    public string Name => "Pipe Draw";

    private BatchCommand? _batch;
    private readonly HashSet<Vector2i> _placedThisStroke = new();

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        _batch = new BatchCommand();
        _placedThisStroke.Clear();
        PlacePipe(ctx, tilePos);
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos)
    {
        PlacePipe(ctx, tilePos);
    }

    public void OnMouseUp(ToolContext ctx)
    {
        if (_batch != null && _batch.Count > 0)
            ctx.CommandStack.Push(_batch);

        _batch = null;
        _placedThisStroke.Clear();
    }

    private void PlacePipe(ToolContext ctx, Vector2i tilePos)
    {
        if (_batch == null)
            return;

        if (!_placedThisStroke.Add(tilePos))
            return;

        if (string.IsNullOrEmpty(ctx.SelectedPipePrototype))
            return;

        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);

        // GridTileToLocal returns tile center (adds TileSizeHalfVector).
        var coords = ctx.MapSystem.GridTileToLocal(gridUid, grid, tilePos);

        var uid = ctx.EntityManager.SpawnEntity(ctx.SelectedPipePrototype, coords);

        // Apply placement rotation to the pipe.
        if (ctx.PlacementRotation != Angle.Zero)
        {
            var xformSystem = ctx.EntityManager.System<SharedTransformSystem>();
            xformSystem.SetLocalRotation(uid, ctx.PlacementRotation);
        }

        var cmd = new SpawnEntityCommand(ctx.EntityManager, uid);
        _batch.Add(cmd);
    }
}
