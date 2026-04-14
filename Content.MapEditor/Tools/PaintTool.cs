using System.Collections.Generic;
using Content.MapEditor.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Paints the selected tile type onto the grid under the cursor.
///     Collects all tile changes during a drag stroke into a single BatchCommand for undo.
/// </summary>
public sealed class PaintTool : IEditorTool
{
    public string Name => "Paint";

    private BatchCommand? _batch;
    private readonly HashSet<Vector2i> _paintedThisStroke = new();

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos, EntityUid gridUid)
    {
        _batch = new BatchCommand();
        _paintedThisStroke.Clear();
        PaintTile(ctx, tilePos, gridUid);
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos, EntityUid gridUid)
    {
        PaintTile(ctx, tilePos, gridUid);
    }

    public void OnMouseUp(ToolContext ctx)
    {
        if (_batch != null && _batch.Count > 0)
        {
            // Tiles were already applied during the stroke for immediate visual feedback.
            // Push without re-executing so undo has the full batch.
            ctx.CommandStack.Push(_batch);
        }

        _batch = null;
        _paintedThisStroke.Clear();
    }

    private void PaintTile(ToolContext ctx, Vector2i pos, EntityUid gridUid)
    {
        if (_batch == null)
            return;

        if (!_paintedThisStroke.Add(pos))
            return; // Already painted this position in the current stroke.

        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var oldTile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

        if (oldTile == ctx.SelectedTile)
            return; // No change needed.

        var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, oldTile, ctx.SelectedTile);
        cmd.Execute(); // Apply immediately for visual feedback.
        _batch.Add(cmd);
    }
}
