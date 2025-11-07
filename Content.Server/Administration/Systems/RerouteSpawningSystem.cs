using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
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
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    private Dictionary<ICommonSession, EntityUid> _catalog = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var session = ev.Player;

        // Check if any Reroute Spawning rules are running
        var rules = EntityQueryEnumerator<RerouteSpawningRuleComponent,GameRuleComponent>();
        while (rules.MoveNext(out var uid, out var reroute, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            // Check if the player already has a map
            if (RequestExistingStation(session, out var stationExist))
            {
                SpawnPlayer(ev, stationExist.Value);
                break;
            }

            // TODO Ask the Manager for a map prototype that the player pricked pre-join or even pre-round?

            if (!_proto.TryIndex(reroute.Map, out var map))
            {
                Log.Error($"Invalid map prototype: {reroute.Map}");
                continue;
            }

            // Create the new map
            GameTicker.LoadGameMap(map, out var mapId, stationName: ev.Profile.Name);
            _map.InitializeMap(mapId);

            // Identify the new station grid that was created
            var spawns = EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent>();
            EntityUid? stationEnt = null;
            while (spawns.MoveNext(out var station, out _, out _))
            {
                stationEnt = station;
            }

            if (stationEnt is null)
            {
                Log.Error($"Reroute failed for {session} - no target station");
                continue;
            }

            var stationTarget = stationEnt.Value;

            //store the newly created station entity for this session so we can put the player back on reconnect
            _catalog.Add(session, stationTarget);

            SpawnPlayer(ev, stationTarget);

            // _outfitSystem.SetOutfit(mob, dm.Gear); //TODO:ERRANT gear??

            ev.Handled = true;
            break;
        }
    }

    private void SpawnPlayer(PlayerBeforeSpawnEvent ev, EntityUid station)
    {
        //TODO This needs a full review, it might be missing modern stuff/steps. Ideally it should call an existing spawningsystem?

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
}
