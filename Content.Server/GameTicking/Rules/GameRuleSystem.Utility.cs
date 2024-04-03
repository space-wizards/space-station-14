using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Shared.Maps;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> where T: IComponent
{
    protected EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
    }

    protected bool TryRoundStartAttempt(RoundStartAttemptEvent ev, string localizedPresetName)
    {
        var query = EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
        while (query.MoveNext(out _, out _, out _, out var gameRule))
        {
            var minPlayers = gameRule.MinPlayers;
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                ChatManager.SendAdminAnnouncement(Loc.GetString("preset-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers),
                    ("presetName", localizedPresetName)));
                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                ChatManager.DispatchServerAnnouncement(Loc.GetString("preset-no-one-ready"));
                ev.Cancel();
            }
        }

        return !ev.Cancelled;
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
    /// <summary>
    ///     Get a random station tile that are neither space tiles nor air-blocked tiles
    /// </summary>
    protected bool TryFindRandomTileOnStation(Entity<StationDataComponent> targetStation, out Vector2i tile, out EntityUid targetGrid, out EntityCoordinates targetCoords)
    {
        tile = default;
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;

        var randomTiles = FindRandomTilesOnStation(targetStation, numberOfTiles: 1);
        if (randomTiles.Count() == 0)
        {
            return false;
        }
        var randomTile = randomTiles.ElementAt(1);

        if (!TryComp<MapGridComponent>(randomTile.GridUid, out var gridComp))
        {
            return false;
        }

        tile = randomTile.GridIndices;
        targetGrid = randomTile.GridUid;
        targetCoords = _map.GridTileToLocal(targetGrid, gridComp, tile);

        return true;
    }
    /// <summary>
    ///     Get [numberOfTiles] random station tiles, with replacement, that are neither space tiles nor air-blocked tiles
    /// </summary>
    protected IEnumerable<TileRef> FindRandomTilesOnStation(Entity<StationDataComponent> station, int numberOfTiles)
    {
        var allValidStationTiles = GetAllVaildStationTiles(station).ToList();
        var allValidStationTilesCount = allValidStationTiles.Count();
        for (var i = 0; i < numberOfTiles; i++)
        {
            var randomIndex = RobustRandom.Next(allValidStationTilesCount);
            yield return allValidStationTiles.ElementAt(randomIndex);
        }
    }
    /// <summary>
    ///     Get all station tiles that are neither space tiles nor air-blocked tiles
    /// </summary>
    protected IEnumerable<TileRef> GetAllVaildStationTiles(Entity<StationDataComponent> station)
    {
        var stationGrids = station.Comp.Grids;
        foreach (var grid in stationGrids)
        {
            if (!TryComp<MapGridComponent>(grid, out var gridComp))
            {
                continue;
            }
            foreach (var tile in _map.GetAllTiles(grid, gridComp))
            {
                if (tile.IsSpace() || _atmosphere.IsTileAirBlocked(grid, tile.GridIndices, mapGridComp: gridComp))
                {
                    continue;
                }
                yield return tile;
            }
        }
    }
}
