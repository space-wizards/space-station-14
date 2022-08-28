using System.Linq;
using Content.Server.CharacterAppearance.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind.Components;
using Content.Server.Nuke;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.MobState;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.Traitor;
using Content.Shared.MobState.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

public sealed class NukeopsRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IPlayerManager _playerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private bool _opsWon;

    private MapId? _nukiePlanet;
    private EntityUid? _nukieOutpost;
    private EntityUid? _nukieShuttle;

    public override string Prototype => "Nukeops";

    private NukeopsRuleConfiguration _nukeopsRuleConfig = new();

    /// <summary>
    ///     Cached starting gear prototypes.
    /// </summary>
    private readonly Dictionary<string, StartingGearPrototype> _startingGearPrototypes = new ();

    /// <summary>
    ///     Cached operator name prototypes.
    /// </summary>
    private readonly Dictionary<string, List<string>> _operativeNames = new();

    /// <summary>
    ///     Data to be used in <see cref="OnMindAdded"/> for an operative once the Mind has been added.
    /// </summary>
    private readonly Dictionary<EntityUid, string> _operativeMindPendingData = new();

    /// <summary>
    ///     Players who played as an operative at some point in the round.
    /// </summary>
    private readonly HashSet<IPlayerSession> _operativePlayers = new();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        SubscribeLocalEvent<NukeOperativeComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
        SubscribeLocalEvent<NukeOperativeComponent, GhostRoleSpawnerUsedEvent>(OnPlayersGhostSpawning);
        SubscribeLocalEvent<NukeOperativeComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<NukeOperativeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<NukeOperativeComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, NukeOperativeComponent component, ComponentInit args)
    {
        // If entity has a prior mind attached, add them to the players list.
        if (!TryComp<MindComponent>(uid, out var mindComponent) || !RuleAdded)
            return;

        var session = mindComponent.Mind?.Session;
        if (session != null)
            _operativePlayers.Add(session);
    }

    private void OnComponentRemove(EntityUid uid, NukeOperativeComponent component, ComponentRemove args)
    {
        CheckRoundShouldEnd();
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
    	if (!RuleAdded)
            return;

        _opsWon = true;
        _roundEndSystem.EndRound();
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        ev.AddLine(_opsWon ? Loc.GetString("nukeops-ops-won") : Loc.GetString("nukeops-crew-won"));
        ev.AddLine(Loc.GetString("nukeops-list-start"));
        foreach (var nukeop in _operativePlayers)
        {
            ev.AddLine($"- {nukeop.Name}");
        }
    }

    private void CheckRoundShouldEnd()
    {
        if (!RuleAdded)
            return;

        MapId? shuttleMapId = EntityManager.EntityExists(_nukieShuttle)
            ? Transform(_nukieShuttle!.Value).MapID
            : null;

        // Check if there are nuke operatives still alive on the same map as the shuttle.
        // If there are, the round can continue.
        var operatives = EntityQuery<NukeOperativeComponent, MobStateComponent, TransformComponent>(true);
        var operativesAlive = operatives
            .Where(ent => ent.Item3.MapID == shuttleMapId)
            .Any(ent => ent.Item2.CurrentState == DamageState.Alive && ent.Item1.Running);

        if (operativesAlive)
            return; // There are living operatives than can access the shuttle.

        // Check that there are spawns available and that they can access the shuttle.
        var spawnsAvailable = EntityQuery<NukeOperativeSpawnerComponent>(true).Any();
        if (spawnsAvailable && shuttleMapId == _nukiePlanet)
            return; // Ghost spawns can still access the shuttle. Continue the round.

        // The shuttle is inaccessible to both living nuke operatives and yet to spawn nuke operatives.
        _roundEndSystem.EndRound();
    }

    private void OnMobStateChanged(EntityUid uid, NukeOperativeComponent component, MobStateChangedEvent ev)
    {
        if(ev.CurrentMobState == DamageState.Dead)
            CheckRoundShouldEnd();
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        if (!RuleAdded)
            return;

        // Basically copied verbatim from traitor code
        var playersPerOperative = _nukeopsRuleConfig.PlayersPerOperative;
        var maxOperatives = _nukeopsRuleConfig.MaxOperatives;

        var everyone = new List<IPlayerSession>(ev.PlayerPool);
        var prefList = new List<IPlayerSession>();
        var cmdrPrefList = new List<IPlayerSession>();
        var operatives = new List<IPlayerSession>();

        // The LINQ expression ReSharper keeps suggesting is completely unintelligible so I'm disabling it
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var player in everyone)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
            {
                continue;
            }
            var profile = ev.Profiles[player.UserId];
            if (profile.AntagPreferences.Contains(_nukeopsRuleConfig.OperativeRoleProto))
            {
                prefList.Add(player);
            }
            if (profile.AntagPreferences.Contains(_nukeopsRuleConfig.CommanderRolePrototype))
            {
                cmdrPrefList.Add(player);
            }
        }

        var numNukies = MathHelper.Clamp(ev.PlayerPool.Count / playersPerOperative, 1, maxOperatives);

        for (var i = 0; i < numNukies; i++)
        {
            IPlayerSession nukeOp;
            // Only one commander, so we do it at the start
            if (i == 0)
            {
                if (cmdrPrefList.Count == 0)
                {
                    if (prefList.Count == 0)
                    {
                        if (everyone.Count == 0)
                        {
                            Logger.InfoS("preset", "Insufficient ready players to fill up with nukeops, stopping the selection");
                            break;
                        }
                        nukeOp = _random.PickAndTake(everyone);
                        Logger.InfoS("preset", "Insufficient preferred nukeop commanders or nukies, picking at random.");
                    }
                    else
                    {
                        nukeOp = _random.PickAndTake(prefList);
                        everyone.Remove(nukeOp);
                        Logger.InfoS("preset", "Insufficient preferred nukeop commanders, picking at random from regular op list.");
                    }
                }
                else
                {
                    nukeOp = _random.PickAndTake(cmdrPrefList);
                    everyone.Remove(nukeOp);
                    prefList.Remove(nukeOp);
                    Logger.InfoS("preset", "Selected a preferred nukeop commander.");
                }
            }
            else
            {
                if (prefList.Count == 0)
                {
                    if (everyone.Count == 0)
                    {
                        Logger.InfoS("preset", "Insufficient ready players to fill up with nukeops, stopping the selection");
                        break;
                    }
                    nukeOp = _random.PickAndTake(everyone);
                    Logger.InfoS("preset", "Insufficient preferred nukeops, picking at random.");
                }
                else
                {
                    nukeOp = _random.PickAndTake(prefList);
                    everyone.Remove(nukeOp);
                    Logger.InfoS("preset", "Selected a preferred nukeop.");
                }
            }
            operatives.Add(nukeOp);
        }

        SpawnOperatives(numNukies, operatives, false);

        foreach(var session in operatives)
        {
            ev.PlayerPool.Remove(session);
            GameTicker.PlayerJoinGame(session);
            _operativePlayers.Add(session);
        }

        if (_nukeopsRuleConfig.GreetSound == null)
            return;

        _audioSystem.PlayGlobal(_nukeopsRuleConfig.GreetSound, Filter.Empty().AddPlayers(operatives), AudioParams.Default);
    }

    private void OnPlayersGhostSpawning(EntityUid uid, NukeOperativeComponent component, GhostRoleSpawnerUsedEvent args)
    {
        var spawner = args.Spawner;

        if (!TryComp<NukeOperativeSpawnerComponent>(spawner, out var nukeOpSpawner))
            return;

        SetupOperativeEntity(uid, nukeOpSpawner.OperativeName, nukeOpSpawner.OperativeStartingGear);

        _operativeMindPendingData.Add(uid, nukeOpSpawner.OperativeRolePrototype);
    }

    private void OnMindAdded(EntityUid uid, NukeOperativeComponent component, MindAddedMessage args)
    {
        if (!TryComp<MindComponent>(uid, out var mindComponent) || mindComponent.Mind == null)
            return;

        var mind = mindComponent.Mind;

        if (_operativeMindPendingData.TryGetValue(uid, out var role))
        {
            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(role)));
            _operativeMindPendingData.Remove(uid);
        }

        if (!mind.TryGetSession(out var playerSession))
            return;

        _operativePlayers.Add(playerSession);

        if (_nukeopsRuleConfig.GreetSound != null)
            _audioSystem.PlayGlobal(_nukeopsRuleConfig.GreetSound, Filter.Empty().AddPlayer(playerSession), AudioParams.Default);
    }

    private bool SpawnMap()
    {
        if (_nukiePlanet != null)
            return true; // Map is already loaded.

        var path = _nukeopsRuleConfig.NukieOutpostMap;
        var shuttlePath = _nukeopsRuleConfig.NukieShuttleMap;
        if (path == null)
        {
            Logger.ErrorS("nukies", "No station map specified for nukeops!");
            return false;
        }

        if (shuttlePath == null)
        {
            Logger.ErrorS("nukies", "No shuttle map specified for nukeops!");
            return false;
        }

        var mapId = _mapManager.CreateMap();

        var (_, outpostGrids) = _mapLoader.LoadMap(mapId, path.ToString());
        if (outpostGrids.Count == 0)
        {
            Logger.ErrorS("nukies", $"Error loading map {path} for nukies!");
            return false;
        }

        // Assume the first grid is the outpost grid.
        _nukieOutpost = outpostGrids[0];

        // Listen I just don't want it to overlap.
        var (_, shuttleId) = _mapLoader.LoadGrid(mapId, shuttlePath.ToString(), new MapLoadOptions()
        {
            Offset = Vector2.One * 1000f,
        });

        // Naughty, someone saved the shuttle as a map.
        if (Deleted(shuttleId))
        {
            Logger.ErrorS("nukeops", $"Tried to load nukeops shuttle as a map, aborting.");
            _mapManager.DeleteMap(mapId);
            return false;
        }

        if (TryComp<ShuttleComponent>(shuttleId, out var shuttle))
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShuttleSystem>().TryFTLDock(shuttle, _nukieOutpost.Value);
        }

        _nukiePlanet = mapId;
        _nukieShuttle = shuttleId;

        return true;
    }

    private (string Name, string Role, string Gear) GetOperativeSpawnDetails(int spawnNumber)
    {
        string name;
        string role;
        string gear;

        // Spawn the Commander then Agent first.
        switch (spawnNumber)
        {
            case 0:
                name = $"Commander " + _random.PickAndTake(_operativeNames[_nukeopsRuleConfig.EliteNames]);
                role = _nukeopsRuleConfig.CommanderRolePrototype;
                gear = _nukeopsRuleConfig.CommanderStartGearPrototype;
                break;
            case 1:
                name = $"Agent " + _random.PickAndTake(_operativeNames[_nukeopsRuleConfig.NormalNames]);
                role = _nukeopsRuleConfig.OperativeRoleProto;
                gear = _nukeopsRuleConfig.MedicStartGearPrototype;
                break;
            default:
                name = $"Operator " + _random.PickAndTake(_operativeNames[_nukeopsRuleConfig.NormalNames]);
                role = _nukeopsRuleConfig.OperativeRoleProto;
                gear = _nukeopsRuleConfig.OperativeStartGearPrototype;
                break;
        }

        return (name, role, gear);
    }

    /// <summary>
    ///     Adds missing nuke operative components, equips starting gear and renames the entity.
    /// </summary>
    private void SetupOperativeEntity(EntityUid mob, string name, string gear)
    {
        EntityManager.GetComponent<MetaDataComponent>(mob).EntityName = name;
        EntityManager.EnsureComponent<RandomHumanoidAppearanceComponent>(mob);
        EntityManager.EnsureComponent<NukeOperativeComponent>(mob);

        if(_startingGearPrototypes.TryGetValue(gear, out var gearPrototype))
            _stationSpawningSystem.EquipStartingGear(mob, gearPrototype, null);
    }

    private void SpawnOperatives(int spawnCount, List<IPlayerSession> sessions, bool addSpawnPoints)
    {
        if (_nukieOutpost == null)
            return;

        var outpostUid = _nukieOutpost.Value;
        var spawns = new List<EntityCoordinates>();

        // Forgive me for hardcoding prototypes
        foreach (var (_, meta, xform) in EntityManager.EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != _nukeopsRuleConfig.SpawnPointPrototype)
                continue;

            if (xform.ParentUid != _nukieOutpost)
                continue;

            spawns.Add(xform.Coordinates);
            break;
        }

        if (spawns.Count == 0)
        {
            spawns.Add(EntityManager.GetComponent<TransformComponent>(outpostUid).Coordinates);
            Logger.WarningS("nukies", $"Fell back to default spawn for nukies!");
        }

        // TODO: This should spawn the nukies in regardless and transfer if possible; rest should go to shot roles.
        for(var i = 0; i < spawnCount; i++)
        {
            var spawnDetails = GetOperativeSpawnDetails(i);
            var nukeOpsAntag = _prototypeManager.Index<AntagPrototype>(spawnDetails.Role);

            if (sessions.TryGetValue(i, out var session))
            {
                var mob = EntityManager.SpawnEntity(_nukeopsRuleConfig.SpawnEntityPrototype, _random.Pick(spawns));
                SetupOperativeEntity(mob, spawnDetails.Name, spawnDetails.Gear);

                var newMind = new Mind.Mind(session.UserId)
                {
                    CharacterName = spawnDetails.Name
                };
                newMind.ChangeOwningPlayer(session.UserId);
                newMind.AddRole(new TraitorRole(newMind, nukeOpsAntag));

                newMind.TransferTo(mob);
            }
            else if (addSpawnPoints)
            {
                var spawnPoint = EntityManager.SpawnEntity(_nukeopsRuleConfig.GhostSpawnPointProto, _random.Pick(spawns));
                var spawner = EnsureComp<GhostRoleMobSpawnerComponent>(spawnPoint);
                spawner.RoleName = nukeOpsAntag.Name;
                spawner.RoleDescription = nukeOpsAntag.Objective;

                var nukeOpSpawner = EnsureComp<NukeOperativeSpawnerComponent>(spawnPoint);
                nukeOpSpawner.OperativeName = spawnDetails.Name;
                nukeOpSpawner.OperativeRolePrototype = spawnDetails.Role;
                nukeOpSpawner.OperativeStartingGear = spawnDetails.Gear;
            }
        }
    }

    private void SpawnOperativesForGhostRoles()
    {
        // Basically copied verbatim from traitor code
        var playersPerOperative = _nukeopsRuleConfig.PlayersPerOperative;
        var maxOperatives = _nukeopsRuleConfig.MaxOperatives;

        var playerPool = _playerSystem.ServerSessions.ToList();
        var numNukies = MathHelper.Clamp(playerPool.Count / playersPerOperative, 1, maxOperatives);

        var operatives = new List<IPlayerSession>();
        SpawnOperatives(numNukies, operatives, true);
    }

    //For admins forcing someone to nukeOps.
    public void MakeLoneNukie(Mind.Mind mind)
    {
        if (!mind.OwnedEntity.HasValue)
            return;

        mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(_nukeopsRuleConfig.OperativeRoleProto)));
        _stationSpawningSystem.EquipStartingGear(mind.OwnedEntity.Value, _prototypeManager.Index<StartingGearPrototype>("SyndicateOperativeGearFull"), null);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        var minPlayers = _nukeopsRuleConfig.MinPlayers;
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("nukeops-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length != 0)
            return;

        _chatManager.DispatchServerAnnouncement(Loc.GetString("nukeops-no-one-ready"));
        ev.Cancel();
    }

    public override void Started()
    {
        _opsWon = false;
        _nukieOutpost = null;
        _nukiePlanet = null;

        _startingGearPrototypes.Clear();
        _operativeNames.Clear();
        _operativeMindPendingData.Clear();
        _operativePlayers.Clear();

        if (Configuration is not NukeopsRuleConfiguration)
            return;

        // TODO: Loot table or something
        foreach (var proto in new[]
                 {
                     _nukeopsRuleConfig.CommanderStartGearPrototype,
                     _nukeopsRuleConfig.MedicStartGearPrototype,
                     _nukeopsRuleConfig.OperativeStartGearPrototype
                 })
        {
            _startingGearPrototypes.Add(proto, _prototypeManager.Index<StartingGearPrototype>(proto));
        }

        foreach (var proto in new[] { _nukeopsRuleConfig.EliteNames, _nukeopsRuleConfig.NormalNames })
        {
            _operativeNames.Add(proto, new List<string>(_prototypeManager.Index<DatasetPrototype>(proto).Values));
        }


        if (!SpawnMap())
        {
            Logger.InfoS("nukies", "Failed to load map for nukeops");
            return;
        }

        // Add pre-existing nuke operatives to the credit list.
        var query = EntityQuery<NukeOperativeComponent, MindComponent>(true);
        foreach (var (_, mindComp) in query)
        {
            if (mindComp.Mind?.TryGetSession(out var session) == true)
                _operativePlayers.Add(session);
        }

        if (GameTicker.RunLevel == GameRunLevel.InRound)
            SpawnOperativesForGhostRoles();
    }

    public override void Ended() { }
}
