using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Linguini.Shared.Util;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawning;

/// <summary>
/// This system can hijack a player before spawning, and put them on their own personal map instead.
/// </summary>
public sealed class RerouteSpawningSystem : GameRuleSystem<RerouteSpawningRuleComponent>
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    private readonly Dictionary<ICommonSession, EntityUid> _stations = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    /// <summary>
    /// A player is trying to spawn into the round
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
            // Even if we give the lobby UI the options, can we actually be sure those will be the choices once the rule starts?
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
        Log.Warning($"Solo spawn failed for {session.Name}, spawning on the normal map.");
    }

    /// <summary>
    /// Create a personal map for every single player that logs in (whatever grid they try to spawn on)
    /// </summary>
    /// <param name="args">The spawn event</param>
    /// <param name="reroute">The reroute component for the event</param>
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
        var stationName = Loc.GetString("solo-station-name", ("character", args.Profile.Name));
        var mapName = Loc.GetString("solo-map-name", ("character", args.Profile.Name));
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
    private void SpawnPlayer(PlayerBeforeSpawnEvent args, ProtoId<JobPrototype>? jobId, EntityUid station)
    {
        //TODO:ERRANT Ideally it should call an existing spawningsystem?

        // You must create a vessel.
        var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(station, jobId, args.Profile);
        var mob = mobMaybe!.Value;
        // Are you there?
        var newMind = _mind.CreateMind(args.Player.UserId, args.Profile.Name);
        _mind.SetUserId(newMind, args.Player.UserId);
        // Are we connected?
        _mind.TransferTo(newMind, mob);
        _roles.MindAddJobRole(newMind, jobPrototype: jobId );
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
        //TODO map cleanup x minutes after the player left the server, or if they go back to the lobby
    }
}
