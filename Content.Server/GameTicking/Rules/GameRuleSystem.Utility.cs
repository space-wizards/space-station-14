using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Station.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Station.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> where T: IComponent
{
    protected EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
    }

    protected EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent> QueryDelayedRules()
    {
        return EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent>();
    }

    /// <summary>
    /// Queries all gamerules, regardless of if they're active or not.
    /// </summary>
    protected EntityQueryEnumerator<T, GameRuleComponent> QueryAllRules()
    {
        return EntityQueryEnumerator<T, GameRuleComponent>();
    }

    /// <summary>
    ///     Utility function for finding a random event-eligible station entity
    /// </summary>
    protected bool TryGetRandomStation([NotNullWhen(true)] out EntityUid? station, Func<EntityUid, bool>? filter = null)
    {
        var stations = new ValueList<EntityUid>(Count<StationEventEligibleComponent>());

        filter ??= _ => true;
        var query = AllEntityQuery<StationEventEligibleComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!filter(uid))
                continue;

            stations.Add(uid);
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        // TODO: Engine PR.
        station = stations[RobustRandom.Next(stations.Count)];
        return true;
    }

    protected bool TryFindRandomTile(out Vector2i tile,
        [NotNullWhen(true)] out EntityUid? targetStation,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetStation = EntityUid.Invalid;
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;
        if (TryGetRandomStation(out targetStation))
        {
            return TryFindRandomTileOnStation((targetStation.Value, Comp<StationDataComponent>(targetStation.Value)),
                out tile,
                out targetGrid,
                out targetCoords);
        }

        return false;
    }

    protected bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
        out Vector2i tile,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords,
        int numAttempts = 10)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;
        targetGrid = EntityUid.Invalid;

        // Weight grid choice by tilecount
        var totalTiles = 0;
        var grids = new List<(Entity<MapGridComponent> Entity, int Count, List<TileRef> Tiles)>();
        foreach (var possibleTarget in station.Comp.Grids)
        {
            if (!TryComp<MapGridComponent>(possibleTarget, out var comp))
                continue;

            // Get the tile count for the given grid.
            var tileCount = _map.GetFilledTileCount((possibleTarget, comp));

            // Just to be sure, no empty elements.
            if (tileCount > 0)
            {
                grids.Add(((possibleTarget, comp), tileCount, new()));
                totalTiles += tileCount;
            }
        }

        if (grids.Count == 0)
        {
            targetGrid = EntityUid.Invalid;
            return false;
        }

        for (var i = 0; i < numAttempts; i++)
        {
            // Find random tile within list.
            var nextTileIndex = RobustRandom.Next(totalTiles);
            TileRef? randomTileRef = null;
            MapGridComponent gridComp = default!;
            var startIndex = 0;
            for (int j = 0; j < grids.Count; j++)
            {
                var grid = grids[j];
                // If the index is in this particular grid, find it and remove the tile to prevent selecting it twice.
                if (nextTileIndex >= startIndex + grid.Count)
                {
                    startIndex += grid.Count;
                    continue;
                }

                (targetGrid, gridComp) = grid.Entity;

                // Empty list: hasn't been queried yet - get our tiles.
                if (grid.Tiles.Count <= 0)
                {
                    grid.Tiles = _map.GetAllTiles(targetGrid, gridComp).ToList();

                    // Actual list count doesn't match expected count (a bug - return failure).
                    Debug.Assert(grid.Tiles.Count == grid.Count);
                    if (grid.Tiles.Count != grid.Count)
                        return false;
                }

                var ourTileIndex = nextTileIndex - startIndex;
                randomTileRef = grid.Tiles[ourTileIndex];
                grid.Tiles.RemoveSwap(ourTileIndex);
                grid.Count--;
                totalTiles--;

                // Empty list, remove element
                if (grid.Tiles.Count <= 0)
                    grids.RemoveSwap(j);

                break;
            }

            // Out of valid tiles, return early.
            if (randomTileRef is not { } tileRef)
                return false;

            // Invalid tile, try again.
            if (_atmosphere.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tileRef.GridIndices)
                || _atmosphere.IsTileAirBlockedCached(targetGrid, tile))
            {
                continue;
            }

            targetCoords = _map.GridTileToLocal(targetGrid, gridComp, tile);
            return true;
        }

        return false;
    }

    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        GameTicker.EndGameRule(uid, component);
    }
}
