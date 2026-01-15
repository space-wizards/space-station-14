using System.Numerics;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Maps;

/// <summary>
/// This system handles transferring <see cref="TileHistoryComponent"/> data when a grid is split.
/// </summary>
public sealed class TileGridSplitSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
    }

    /// <summary>
    /// Transfer tile history from the old grid to the new grids.
    /// </summary>
    private void OnGridSplit(ref GridSplitEvent ev)
    {
        if (!TryComp<TileHistoryComponent>(ev.Grid, out var oldHistory))
            return;

        var oldGrid = Comp<MapGridComponent>(ev.Grid);

        foreach (var gridUid in ev.NewGrids)
        {
            // ensure the new grid has a history component and get its grid component
            var newHistory = EnsureComp<TileHistoryComponent>(gridUid);
            var newGrid = Comp<MapGridComponent>(gridUid);

            foreach (var tile in _maps.GetAllTiles(gridUid, newGrid))
            {
                // calculate where this tile was on the old grid
                var oldIndices = _maps.LocalToTile(ev.Grid, oldGrid, new EntityCoordinates(gridUid, new Vector2(tile.GridIndices.X + 0.5f, tile.GridIndices.Y + 0.5f)));

                var chunkIndices = SharedMapSystem.GetChunkIndices(oldIndices, TileSystem.ChunkSize);
                if (oldHistory.ChunkHistory.TryGetValue(chunkIndices, out var oldChunk) &&
                    oldChunk.History.TryGetValue(oldIndices, out var history))
                {
                    // now we move the history from the old grid to the new grid
                    var newChunkIndices = SharedMapSystem.GetChunkIndices(tile.GridIndices, TileSystem.ChunkSize);
                    if (!newHistory.ChunkHistory.TryGetValue(newChunkIndices, out var newChunk))
                    {
                        newChunk = new TileHistoryChunk();
                        newHistory.ChunkHistory[newChunkIndices] = newChunk;
                    }

                    newChunk.History[tile.GridIndices] = new List<ProtoId<ContentTileDefinition>>(history);
                    newChunk.LastModified = _timing.CurTick;

                    // clean up the old history
                    oldChunk.History.Remove(oldIndices);
                    if (oldChunk.History.Count == 0)
                        oldHistory.ChunkHistory.Remove(chunkIndices);
                    else
                        oldChunk.LastModified = _timing.CurTick;
                }
            }

            Dirty(gridUid, newHistory);
        }

        Dirty(ev.Grid, oldHistory);
    }
}
