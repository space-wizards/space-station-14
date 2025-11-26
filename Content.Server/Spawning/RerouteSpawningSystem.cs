using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Linguini.Shared.Util;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawning;

// TODO Integration test

/// <summary>
/// This system circumvents the normal spawn system, and puts each player on their own personal map as the pre-specified job.
/// </summary>
public sealed class RerouteSpawningSystem : GameRuleSystem<RerouteSpawningRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly Dictionary<ICommonSession, EntityUid> _stations = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    /// <summary>
    /// A player is trying to enter the round, or is respawning
    /// </summary>
    private void OnBeforeSpawn(PlayerBeforeSpawnEvent args)
    {
        var session = args.Player;

        // Check if any Reroute Spawning rules are running
        var rules = EntityQueryEnumerator<RerouteSpawningRuleComponent,GameRuleComponent>();
        while (rules.MoveNext(out var uid, out var reroute, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            //TODO Lobby selection UI. Problem: the gamerule DOES NOT EXIST YET in the lobby.
            //Even if we give the lobby UI the options, can we actually be sure those will be the choices once the rule starts?
            var playerChoice = 0;

            if (!playerChoice.InRange(0, reroute.Prototypes.Count - 1))
            {
                Log.Warning($"Invalid player choice from {session} - Selected option '{playerChoice}', but only 0 to {reroute.Prototypes.Count - 1} exist");
                playerChoice = 0;
            }

            var choice = reroute.Prototypes[playerChoice];

            if (!_proto.TryIndex<RerouteSpawningPrototype>(choice, out var reroutePrototype))
            {
                Log.Warning($"Spawn reroute failed for {session} - Reroute prototype '{choice}' does not exist");
                continue;
            }

            var job = reroutePrototype.Job;

            // This allows the player to respawn on their existing map, rather than start a new one
            if (RequestExistingStation(session, out var stationExist))
            {
                SpawnPlayer(args, job, stationExist.Value);
                args.Handled = true;
                break;
            }

            if (!CreateSoloStation(args, reroutePrototype, session, out var stationTarget))
                continue;

            SpawnPlayer(args, job, stationTarget.Value);
            args.Handled = true;
            break;
        }

        if (args.Handled == false)
            Log.Warning($"Solo spawn failed for {session.Name}, spawning on the normal map.");
    }

    /// <summary>
    /// Create a personal map for one player
    /// </summary>
    /// <param name="args">The spawn event for the player trying to spawn</param>
    /// <param name="reroute">The reroute prototype that specifies the map and job for the spawn</param>
    /// <param name="session">The player session</param>
    /// <param name="stationTarget">The station entity (nullspace) that is being created for this player</param>
    private bool CreateSoloStation(
        PlayerBeforeSpawnEvent args,
        RerouteSpawningPrototype reroute,
        ICommonSession session,
        [NotNullWhen(true)]out EntityUid? stationTarget)
    {
        stationTarget = null;
        var proto = reroute.Map;

        if (!_proto.TryIndex(proto, out var map))
        {
            Log.Error($"Spawn reroute failed for {session} - Invalid map prototype: {proto}");
            return false;
        }

        // Create the new map and station, and assign them identifiable names
        var stationName = Loc.GetString("training-station-name", ("character", args.Profile.Name));
        var mapName = Loc.GetString("training-map-name", ("character", args.Profile.Name));
        var query = GameTicker.LoadGameMap(map, out var mapId, stationName: stationName);
        var newMap = query.First();
        _meta.SetEntityName(Transform(newMap).ParentUid, mapName);

        _map.InitializeMap(mapId);

        if (!TryComp<StationMemberComponent>(newMap, out var member))
        {
            Log.Error($"Spawn reroute failed for {session} - Target station not found");
            return false;
        }

        stationTarget = member.Station;

        //store the newly created station entity for this session so we can put the player back on respawn
        _stations.Add(session, stationTarget.Value);
        return true;
    }

    /// <summary>
    /// Spawn the player and their gear
    /// </summary>
    /// <param name="args">The spawn event</param>
    /// <param name="jobId">The prototype ID of the job that will be assigned to the player</param>
    /// <param name="station">The station entity (nullspace)</param>
    private void SpawnPlayer(PlayerBeforeSpawnEvent args, ProtoId<JobPrototype> jobId, EntityUid station)
    {
        var session = args.Player;
        var humanoid = args.Profile;

        GameTicker.DoSpawn(session, humanoid, station, jobId, true, out var mob, out _, out var jobName);

        // Latejoin is not a relevant concept for rerouted spawns, when the station did not even exist beforehand
        // Also, round flow does not exist in the regular sense on a tutorial server
        // So all spawns are recorded as just "Joined"
        _adminLogger.Add(LogType.RoundStartJoin,
            LogImpact.Medium,
            $"Player {session.Name} has spawned on a solitary map. Joined as {humanoid.Name:characterName} on station {Name(station):stationName} with {ToPrettyString(mob):entity} as a {jobName:jobName}.");
    }

    /// <summary>
    /// Checks if a player already has a station allocated to them.
    /// </summary>
    /// <param name="session">The player session</param>
    /// <param name="station">The player's existing station</param>
    /// <returns></returns>
    private bool RequestExistingStation(ICommonSession session, [NotNullWhen(true)] out EntityUid? station)
    {
        station = null;

        if (!_stations.TryGetValue(session, out var stored))
            return false;

        station = stored;
        return true;
    }

    /// Clear the saved station list, since the maps are being deleted
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _stations.Clear();
    }

    private void MapCleanup()
    {
        // TODO map cleanup x minutes after the player left the server, or if they go back to the lobby
    }
}
