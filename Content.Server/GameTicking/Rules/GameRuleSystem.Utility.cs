using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Station.Components;
using Content.Shared.GameTicking.Components;
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

    /// <summary>
    /// Get all entities with <see cref="TComponent"/> that are on the station. Ignore entities outside the station.
    /// </summary>
    /// <param name="checkIfAnchored">Whether to only get anchored entities.
    /// Good check for air vents, bad for containers.</param>
    /// <returns>All matching entities.</returns>
    protected HashSet<Entity<TComponent>> GetEntitiesWithComponentOnStation<TComponent>(bool checkIfAnchored) where TComponent : IComponent
    {
        return GetEntitiesWithComponentOnStation<TComponent>(checkIfAnchored, out _);
    }

    /// <param name="station">Optional station to search. If null, a random eligible station is used.</param>
    /// <inheritdoc cref="GetEntitiesWithComponentOnStation{TComponent}(bool)" />
    protected HashSet<Entity<TComponent>> GetEntitiesWithComponentOnStation<TComponent>(bool checkIfAnchored,
        out EntityUid? station)
        where TComponent : IComponent
    {
        HashSet<Entity<TComponent>> entities = [];

        if (!TryGetRandomStation(out station))
        {
            return entities;
        }

        var grid = StationSystem.GetLargestGrid(station.Value);
        if (grid is null)
        {
            return entities;
        }

        var locations = EntityQueryEnumerator<TComponent, TransformComponent>();
        while (locations.MoveNext(out var uid, out var component, out var transform))
        {
            if (checkIfAnchored && !transform.Anchored)
            {
                continue;
            }

            if (transform.GridUid != grid)
            {
                continue;
            }

            entities.Add((uid, component));
        }

        return entities;
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
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;

        var targetGridMaybe = StationSystem.GetLargestGrid(station.Owner);
        if (targetGridMaybe is null)
        {
            return false;
        }

        if (!TryComp<MapGridComponent>(targetGridMaybe.Value, out var comp))
        {
            return false;
        }

        targetGrid = targetGridMaybe.Value;
        var grid = new Entity<MapGridComponent>(targetGrid, comp);

        var gridTiles = _map.GetAllTiles(targetGrid, grid.Comp).ToList();
        var totalTiles = gridTiles.Count;

        for (var i = 0; i < numAttempts; i++)
        {
            var nextTileIndex = RobustRandom.Next(totalTiles);
            var tileRef = gridTiles[nextTileIndex];
            gridTiles.RemoveSwap(nextTileIndex);
            totalTiles--;

            if (totalTiles <= 0)
            {
                break;
            }

            if (_atmosphere.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tileRef.GridIndices)
                || _atmosphere.IsTileAirBlockedCached(targetGrid, tileRef.GridIndices))
            {
                continue;
            }

            targetCoords = _map.GridTileToLocal(targetGrid, grid.Comp, tileRef.GridIndices);
            tile = tileRef.GridIndices;
            return true;
        }

        return false;
    }

    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        GameTicker.EndGameRule(uid, component);
    }
}
