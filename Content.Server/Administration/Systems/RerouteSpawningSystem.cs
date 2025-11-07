using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
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
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    private readonly Dictionary<ICommonSession, EntityUid> _catalog = [];
    private const RerouteType Grouping = RerouteType.Solo;

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

            // Check if the player already has a map
            if (RequestExistingStation(session, out var stationExist))
            {
                SpawnPlayer(args, stationExist.Value);
                break;
            }

            switch (Grouping)
            {
                case RerouteType.Solo:
                    if (!CreateSoloStation(args, reroute, session, out var stationTarget))
                        continue;
                    SpawnPlayer(args, stationTarget.Value);
                    // _outfitSystem.SetOutfit(mob, dm.Gear); //TODO:ERRANT gear??
                    args.Handled = true;
                    break;

                //TODO reroutes that group some players to the same map, based on some criteria?
                //Maybe things like Nukie Spawn could be folded into that code?
            }
        }
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
        RerouteSpawningRuleComponent reroute,
        ICommonSession session,
        [NotNullWhen(true)]out EntityUid? stationTarget)
    {
        stationTarget = null;
        // TODO Ask the Manager for a map prototype that the player pricked pre-join or even pre-round?

        if (!_proto.TryIndex(reroute.Map, out var map))
        {
            Log.Error($"Invalid map prototype: {reroute.Map}");
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
            Log.Error($"Reroute failed for {session} - no target station");
            return false;
        }

        stationTarget = member.Station;

        //store the newly created station entity for this session so we can put the player back on reconnect
        _catalog.Add(session, stationTarget.Value);
        return true;
    }

    /// <summary>
    /// Spawn the player and their gear
    /// </summary>
    /// <param name="args">The spawn event</param>
    /// <param name="station">The station entity (nullspace)</param>
    private void SpawnPlayer(PlayerBeforeSpawnEvent args, EntityUid station)
    {
        //TODO:ERRANT This needs a full review, it might be missing modern stuff/steps. Ideally it should call an existing spawningsystem?

        var newMind = _mind.CreateMind(args.Player.UserId, args.Profile.Name);
        _mind.SetUserId(newMind, args.Player.UserId);
        var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(station, null, args.Profile);
        DebugTools.AssertNotNull(mobMaybe);
        var mob = mobMaybe!.Value;
        _mind.TransferTo(newMind, mob);
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

        if (!_catalog.TryGetValue(session, out var stored))
            return false;

        station = stored;
        return true;
    }

    /// Clear the saved station list, since the maps are being deleted
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _catalog.Clear();
    }
}
