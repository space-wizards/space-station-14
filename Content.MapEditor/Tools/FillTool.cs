using System.Collections.Generic;
using Content.MapEditor.Commands;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.MapEditor.Tools;

/// <summary>
///     Flood-fills all connected tiles of the same type with the selected tile.
///     Uses BFS with a safety cap of 500 tiles to prevent runaway fills.
/// </summary>
public sealed class FillTool : IEditorTool
{
    public string Name => "Fill";

    private const int MaxFillTiles = 500;

    public void OnMouseDown(ToolContext ctx, Vector2i tilePos)
    {
        var gridUid = ctx.ActiveGridUid;
        var grid = ctx.EntityManager.GetComponent<MapGridComponent>(gridUid);
        var originTile = ctx.MapSystem.GetTileRef(gridUid, grid, tilePos).Tile;

        // If the origin tile is already the selected tile type, nothing to do.
        // Compare only TypeId so that different variants/flags don't block the fill.
        if (originTile.TypeId == ctx.SelectedTile.TypeId)
            return;

        var batch = new BatchCommand();
        var visited = new HashSet<Vector2i>();
        var queue = new Queue<Vector2i>();
        queue.Enqueue(tilePos);
        visited.Add(tilePos);

        while (queue.Count > 0 && visited.Count <= MaxFillTiles)
        {
            var pos = queue.Dequeue();
            var currentTile = ctx.MapSystem.GetTileRef(gridUid, grid, pos).Tile;

            if (currentTile.TypeId != originTile.TypeId)
                continue;

            var cmd = new SetTileCommand(ctx.MapSystem, gridUid, grid, pos, currentTile, ctx.GetVariantTile());
            cmd.Execute();
            batch.Add(cmd);

            // Enqueue 4-directional neighbors.
            foreach (var neighbor in GetNeighbors(pos))
            {
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        if (batch.Count > 0)
            ctx.CommandStack.Push(batch);
    }

    public void OnMouseDrag(ToolContext ctx, Vector2i tilePos)
    {
        // No-op: fill is a single-click operation.
    }

    public void OnMouseUp(ToolContext ctx)
    {
        // No-op.
    }

    private static IEnumerable<Vector2i> GetNeighbors(Vector2i pos)
    {
        yield return new Vector2i(pos.X + 1, pos.Y);
        yield return new Vector2i(pos.X - 1, pos.Y);
        yield return new Vector2i(pos.X, pos.Y + 1);
        yield return new Vector2i(pos.X, pos.Y - 1);
    }
}
