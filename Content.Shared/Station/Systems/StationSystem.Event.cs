using System.Diagnostics.CodeAnalysis;
using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Station.Systems;

public abstract partial class StationSystem
{
    /// <summary>
    ///     Utility function for finding a random event-eligible station entity
    ///     TODO: Make calls of this use the below method with T = StationEventEligibleComponent and make this a lookup for NO T versions of stations!!!
    /// </summary>
    [PublicAPI]
    [Obsolete]
    public bool TryGetRandomStation([NotNullWhen(true)] out Entity<StationDataComponent>? station, Func<EntityUid, bool>? filter = null)
    {
        var stations = new ValueList<Entity<StationDataComponent>>(Count<StationEventEligibleComponent>());

        filter ??= _ => true;
        var query = AllEntityQuery<StationEventEligibleComponent, StationDataComponent>();

        while (query.MoveNext(out var uid, out _, out var data))
        {
            if (!filter(uid))
                continue;

            stations.Add((uid, data));
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        // TODO: Engine PR.
        station = stations[Random.Next(stations.Count)];
        return true;
    }

    /// <summary>
    /// Tries to get a random station with a given component.
    /// </summary>
    /// <param name="station">Station we are returning.</param>
    /// <param name="filter">Optional filter.</param>
    /// <typeparam name="T">Component we are querying for.</typeparam>
    /// <returns>Station Entity if it exists</returns>
    [PublicAPI]
    public bool TryGetRandomStation<T>([NotNullWhen(true)] out Entity<StationDataComponent, T>? station, Func<EntityUid, bool>? filter = null) where T : IComponent
    {
        var stations = new ValueList<Entity<StationDataComponent, T>>(Count<T>());

        filter ??= _ => true;
        var query = AllEntityQuery<T, StationDataComponent>();

        while (query.MoveNext(out var uid, out var comp, out var data))
        {
            if (!filter(uid))
                continue;

            stations.Add((uid, data, comp));
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        // TODO: Engine PR.
        station = stations[Random.Next(stations.Count)];
        return true;
    }

    [PublicAPI]
    public bool TryFindRandomTile(out Vector2i tile,
        [NotNullWhen(true)] out Entity<StationDataComponent>? targetStation,
        [NotNullWhen(true)] out Entity<MapGridComponent>? targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetStation = null;
        targetGrid = null;
        targetCoords = EntityCoordinates.Invalid;
        if (TryGetRandomStation(out targetStation))
        {
            return TryFindRandomTileOnStation(targetStation.Value,
                out tile,
                out targetGrid,
                out targetCoords);
        }

        return false;
    }

    /// <summary>
    /// Returns a random tile on a random station with the given T component.
    /// </summary>
    /// <param name="tile">Tile returned.</param>
    /// <param name="targetStation">Station returned</param>
    /// <param name="targetGrid">Station grid with the tile.</param>
    /// <param name="targetCoords">Coordinates of the tile.</param>
    /// <typeparam name="T">Component we query for the station!</typeparam>
    /// <returns>True if all of the above were found</returns>
    [PublicAPI]
    public bool TryFindRandomTile<T>(out Vector2i tile,
        [NotNullWhen(true)] out Entity<StationDataComponent, T>? targetStation,
        [NotNullWhen(true)] out Entity<MapGridComponent>? targetGrid,
        out EntityCoordinates targetCoords) where T : IComponent
    {
        tile = default;
        targetStation = null;
        targetGrid = null;
        targetCoords = EntityCoordinates.Invalid;
        return TryGetRandomStation(out targetStation) && TryFindRandomTileOnStation(targetStation.Value,
            out tile,
            out targetGrid,
            out targetCoords);
    }

    [PublicAPI]
    public virtual bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
        out Vector2i tile,
        [NotNullWhen(true)] out Entity<MapGridComponent>? targetGrid,
        out EntityCoordinates targetCoords,
        int numAttempts = 10)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;
        targetGrid = null;
        return false;
    }
}
