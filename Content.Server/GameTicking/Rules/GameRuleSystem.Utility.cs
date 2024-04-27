using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Shared.Maps;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Serilog.Debugging;

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
    ///     Get a random station tile that is neither a space tile nor an air-blocked tile
    /// </summary>
    protected bool TryFindRandomTileOnStation(Entity<StationDataComponent> targetStation, out Vector2i tile, out EntityUid targetGrid, out EntityCoordinates targetCoords)
    {
        tile = default;
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;
        var grids = targetStation.Comp.Grids;
        TileRef? randomTile = null;

        var gridCount = grids.Count;
        if (gridCount == 0)
        {
            return false;
        }

        var weights = new float[gridCount];
        var mapGrids = new MapGridComponent[gridCount];
        
        int mapGridIndex = 0;
        float weightTotal = 0;

        foreach (var grid in grids)
        {
            if (TryComp<MapGridComponent>(grid, out var mapGrid))
            {
                float curWeight =  mapGrid.LocalAABB.Size.LengthSquared();

                weightTotal += curWeight;
                weights[mapGridIndex] = curWeight;
                mapGrids[mapGridIndex] = mapGrid;

                ++mapGridIndex;
            }
        }

        float randomFloat = RobustRandom.NextFloat(weightTotal);
        MapGridComponent? selectedMapGrid = null;

        for (var i = 0; i < mapGridIndex; ++i)
        {
            randomFloat -= weights[i];
            if (randomFloat <= 0)
            {
                selectedMapGrid = mapGrids[i];
            }
        }

        if (selectedMapGrid is null)
        {
            throw new UnreachableException();
        }

        var found = false;
        var aabb = selectedMapGrid.LocalAABB;
        for (var i = 0; i < 10; i++)
        {
            var randomX = RobustRandom.Next((int) aabb.Left, (int) aabb.Right);
            var randomY = RobustRandom.Next((int) aabb.Bottom, (int) aabb.Top);

            tile = new Vector2i(randomX, randomY);
            if (_atmosphere.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tile)
                || _atmosphere.IsTileAirBlocked(targetGrid, tile, mapGridComp: selectedMapGrid))
            {
                continue;
            }

            found = true;
            targetCoords = _map.GridTileToLocal(targetGrid, selectedMapGrid, tile);
            break;
        }
        return found;
    }
}