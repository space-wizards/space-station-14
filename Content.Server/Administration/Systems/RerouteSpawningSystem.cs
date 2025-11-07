using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Maps;
using Content.Server.Mind;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

/// <summary>
/// This system can hijack a player before spawning, and put them on their own personal map instead.
/// </summary>
public sealed class RerouteSpawningSystem : GameRuleSystem<RerouteSpawningRuleComponent>
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    // [Dependency] private readonly SolitaryManager _solitary = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    private Dictionary<ICommonSession, EntityUid> _catalog = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var session = ev.Player;

        // Copied verbatim from DeathMatchRuleSystem
        // Check if any Reroute Spawning rules are running
        var query = EntityQueryEnumerator<RerouteSpawningRuleComponent,GameRuleComponent>();
        while (query.MoveNext(out var uid, out var reroute, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            // Check if the player already has a map
            if (RequestExistingStation(session, out var stationExist))
            {
                SpawnPlayer(ev, stationExist.Value);
                break;
            }

            // TODO Ask the Manager for a map prototype that the player pricked pre-join or even pre-round

            // TODO This whole stuff should probably work through Gameticker.Roundflow

            if (!_proto.TryIndex(reroute.Map, out var map))
            {
                Log.Warning($"Invalid map prototype: {reroute.Map}");
                continue;
            }

            // Create new map
            // var newMap = _map.CreateMap(out var mapId);
            // _loader.TryLoadGrid(mapId,
            //     ev.GameMap.MapPath,
            //     out var grid,
            //     ev.Options,
            //     ev.Offset,
            //     ev.Rotation))

            var stationGrids = GameTicker.LoadGameMap(map, out var mapId);

            // TODO give this map a custom name




            //TODO:ERRANT I think this is not actually the station, but the map or the grid.
            // THis does not work, and is only here to get the code to continue to Spawn
            // var stationGrid = stationGrids.First();


            _map.InitializeMap(mapId);

            // DebugTools.Assert(!_map.IsInitialized(mapId));



            ///
            var spawns = EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent>();
            EntityUid? stationEnt = null;
            var targets = new List<EntityUid>();
            StationSpawningComponent? spawnTarget;
            while (spawns.MoveNext(out var station, out _, out var spawn))
            {
                // var comp1 = Transform(station);
                // if (comp1.MapID == mapId)
                // {
                    targets.Add(station);
                // }

                var c = 3;
            }
            stationEnt = targets.Last();
            var stationTarget = stationEnt.Value;
            // var stationTarget = stationGrid;

            if (!TryComp<StationSpawningComponent>(stationEnt.Value, out var comp))
            {
                Log.Error($"Reroute failed for {session} - no target station");
                break;
            }

            //store the newly created station entity for this session so we can put the player back on reconnect
            _catalog.Add(session, stationTarget);

            SpawnPlayer(ev, stationTarget);

            // _outfitSystem.SetOutfit(mob, dm.Gear); //TODO:ERRANT gear??

            ev.Handled = true;
            break;
        }
    }

    // TODO:ERRANT this is kinda a duplication for the code in GameTicker. Very much not ideal
    private void LoadMap(PlayerBeforeSpawnEvent ev, GameMapPrototype map)
    {


    }

    private void SpawnPlayer(PlayerBeforeSpawnEvent ev, EntityUid station)
    {
        //TODO:ERRANT this was copied from deathmatchsystem, might be bad/obsolete!
        var newMind = _mind.CreateMind(ev.Player.UserId, ev.Profile.Name);
        _mind.SetUserId(newMind, ev.Player.UserId);
        var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(station, null, ev.Profile);
        DebugTools.AssertNotNull(mobMaybe);
        var mob = mobMaybe!.Value;
        _mind.TransferTo(newMind, mob);
    }

    /// <summary>
    /// Checks if a player already has a station allocated to them.
    /// </summary>
    /// <param name="session">The player session</param>
    /// <param name="station">The player's existing station, if there is one</param>
    /// <returns></returns>
    private bool RequestExistingStation(ICommonSession session, [NotNullWhen(true)] out EntityUid? station)
    {
        station = null;

        if (!_catalog.TryGetValue(session, out var stored))
            return false;

        station = stored;
        return true;
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        // Do I even need this one?
    }

}
