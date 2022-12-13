using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.CharacterAppearance.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Humanoid.Systems;
using Content.Server.Mind.Components;
using Content.Server.NPC.Systems;
using Content.Server.Nuke;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Players;
using Content.Server.Traitor;
using Content.Shared.Dataset;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Nuke;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.CCVar;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class NukeopsRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerSystem = default!;
    [Dependency] private readonly FactionSystem _faction = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;


    private enum WinType
    {
        /// <summary>
        ///     Operative major win. This means they nuked the station.
        /// </summary>
        OpsMajor,
        /// <summary>
        ///     Minor win. All nukies were alive at the end of the round.
        ///     Alternatively, some nukies were alive, but the disk was left behind.
        /// </summary>
        OpsMinor,
        /// <summary>
        ///     Neutral win. The nuke exploded, but on the wrong station.
        /// </summary>
        Neutral,
        /// <summary>
        ///     Crew minor win. The nuclear authentication disk escaped on the shuttle,
        ///     but some nukies were alive.
        /// </summary>
        CrewMinor,
        /// <summary>
        ///     Crew major win. This means they either killed all nukies,
        ///     or the bomb exploded too far away from the station, or on the nukie moon.
        /// </summary>
        CrewMajor
    }

    private enum WinCondition
    {
        NukeExplodedOnCorrectStation,
        NukeExplodedOnNukieOutpost,
        NukeExplodedOnIncorrectLocation,
        NukeActiveInStation,
        NukeActiveAtCentCom,
        NukeDiskOnCentCom,
        NukeDiskNotOnCentCom,
        NukiesAbandoned,
        AllNukiesDead,
        SomeNukiesAlive,
        AllNukiesAlive
    }

    private WinType _winType = WinType.Neutral;

    private WinType RuleWinType
    {
        get => _winType;
        set
        {
            _winType = value;

            if (value == WinType.CrewMajor || value == WinType.OpsMajor)
            {
                _roundEndSystem.EndRound();
            }
        }
    }
    private List<WinCondition> _winConditions = new ();

    private MapId? _nukiePlanet;

    // TODO: use components, don't just cache entity UIDs
    // There have been (and probably still are) bugs where these refer to deleted entities from old rounds.
    private EntityUid? _nukieOutpost;
    private EntityUid? _nukieShuttle;
    private EntityUid? _targetStation;

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
    ///     Stores the session as well as the entity name
    /// </summary>
    private readonly Dictionary<string, IPlayerSession> _operativePlayers = new();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        SubscribeLocalEvent<NukeOperativeComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<NukeDisarmSuccessEvent>(OnNukeDisarm);
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
        var name = MetaData(uid).EntityName;
        if (session != null)
            _operativePlayers.Add(name, session);
    }

    private void OnComponentRemove(EntityUid uid, NukeOperativeComponent component, ComponentRemove args)
    {
        CheckRoundShouldEnd();
    }

    private void OnNukeExploded(NukeExplodedEvent ev)
    {
    	if (!RuleAdded)
            return;

        if (ev.OwningStation != null)
        {
            if (ev.OwningStation == _nukieOutpost)
            {
                _winConditions.Add(WinCondition.NukeExplodedOnNukieOutpost);
                RuleWinType = WinType.CrewMajor;
                return;
            }

            if (TryComp(_targetStation, out StationDataComponent? data))
            {
                foreach (var grid in data.Grids)
                {
                    if (grid != ev.OwningStation)
                    {
                        continue;
                    }

                    _winConditions.Add(WinCondition.NukeExplodedOnCorrectStation);
                    RuleWinType = WinType.OpsMajor;
                    return;
                }
            }

            _winConditions.Add(WinCondition.NukeExplodedOnIncorrectLocation);
        }
        else
        {
            _winConditions.Add(WinCondition.NukeExplodedOnIncorrectLocation);
        }

        _roundEndSystem.EndRound();
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        switch (ev.New)
        {
            case GameRunLevel.InRound:
                OnRoundStart();
                break;
            case GameRunLevel.PostRound:
                OnRoundEnd();
                break;
        }
    }

    private void OnRoundStart()
    {
        // TODO: This needs to try and target a Nanotrasen station. At the very least,
        // we can only currently guarantee that NT stations are the only station to
        // exist in the base game.

        _targetStation = _stationSystem.Stations.FirstOrNull();

        if (_targetStation == null)
        {
            return;
        }

        var filter = Filter.Empty();
        foreach (var nukie in EntityQuery<NukeOperativeComponent>())
        {
            if (!TryComp<ActorComponent>(nukie.Owner, out var actor))
            {
                continue;
            }

            _chatManager.DispatchServerMessage(actor.PlayerSession, Loc.GetString("nukeops-welcome", ("station", _targetStation.Value)));
            filter.AddPlayer(actor.PlayerSession);
        }

        _audioSystem.PlayGlobal(_nukeopsRuleConfig.GreetSound, filter, recordReplay: false);
    }

    private void OnRoundEnd()
    {
        // If the win condition was set to operative/crew major win, ignore.
        if (RuleWinType == WinType.OpsMajor || RuleWinType == WinType.CrewMajor)
        {
            return;
        }

        foreach (var (nuke, nukeTransform) in EntityManager.EntityQuery<NukeComponent, TransformComponent>(true))
        {
            if (nuke.Status != NukeStatus.ARMED)
            {
                continue;
            }

            // UH OH
            if (nukeTransform.MapID == _shuttleSystem.CentComMap)
            {
                _winConditions.Add(WinCondition.NukeActiveAtCentCom);
                RuleWinType = WinType.OpsMajor;
                return;
            }

            if (nukeTransform.GridUid == null || _targetStation == null)
            {
                continue;
            }

            if (!TryComp(_targetStation.Value, out StationDataComponent? data))
            {
                continue;
            }

            foreach (var grid in data.Grids)
            {
                if (grid != nukeTransform.GridUid)
                {
                    continue;
                }

                _winConditions.Add(WinCondition.NukeActiveInStation);
                RuleWinType = WinType.OpsMajor;
                return;
            }
        }

        var allAlive = true;
        foreach (var (_, state) in EntityQuery<NukeOperativeComponent, MobStateComponent>())
        {
            if (state.CurrentState is DamageState.Alive)
            {
                continue;
            }

            allAlive = false;
            break;
        }
        // If all nuke ops were alive at the end of the round,
        // the nuke ops win. This is to prevent people from
        // running away the moment nuke ops appear.
        if (allAlive)
        {
            RuleWinType = WinType.OpsMinor;
            _winConditions.Add(WinCondition.AllNukiesAlive);
            return;
        }

        _winConditions.Add(WinCondition.SomeNukiesAlive);

        var diskAtCentCom = false;
        foreach (var (_, transform) in EntityManager.EntityQuery<NukeDiskComponent, TransformComponent>())
        {
            var diskMapId = transform.MapID;
            diskAtCentCom = _shuttleSystem.CentComMap == diskMapId;

            // TODO: The target station should be stored, and the nuke disk should store its original station.
            // This is fine for now, because we can assume a single station in base SS14.
            break;
        }

        // If the disk is currently at Central Command, the crew wins - just slightly.
        // This also implies that some nuclear operatives have died.
        if (diskAtCentCom)
        {
            RuleWinType = WinType.CrewMinor;
            _winConditions.Add(WinCondition.NukeDiskOnCentCom);
        }
        // Otherwise, the nuke ops win.
        else
        {
            RuleWinType = WinType.OpsMinor;
            _winConditions.Add(WinCondition.NukeDiskNotOnCentCom);
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        var winText = Loc.GetString($"nukeops-{_winType.ToString().ToLower()}");

        ev.AddLine(winText);

        foreach (var cond in _winConditions)
        {
            var text = Loc.GetString($"nukeops-cond-{cond.ToString().ToLower()}");

            ev.AddLine(text);
        }

        ev.AddLine(Loc.GetString("nukeops-list-start"));
        foreach (var (name, session) in _operativePlayers)
        {
            var listing = Loc.GetString("nukeops-list-name", ("name", name), ("user", session.Name));
            ev.AddLine(listing);
        }
    }

    private void CheckRoundShouldEnd()
    {
        if (!RuleAdded || RuleWinType == WinType.CrewMajor || RuleWinType == WinType.OpsMajor)
            return;

        // If there are any nuclear bombs that are active, immediately return. We're not over yet.
        foreach (var nuke in EntityQuery<NukeComponent>())
        {
            if (nuke.Status == NukeStatus.ARMED)
            {
                return;
            }
        }

        MapId? shuttleMapId = EntityManager.EntityExists(_nukieShuttle)
            ? Transform(_nukieShuttle!.Value).MapID
            : null;

        MapId? targetStationMap = null;
        if (_targetStation != null && TryComp(_targetStation, out StationDataComponent? data))
        {
            var grid = data.Grids.FirstOrNull();
            targetStationMap = grid != null
                ? Transform(grid.Value).MapID
                : null;
        }

        // Check if there are nuke operatives still alive on the same map as the shuttle,
        // or on the same map as the station.
        // If there are, the round can continue.
        var operatives = EntityQuery<NukeOperativeComponent, MobStateComponent, TransformComponent>(true);
        var operativesAlive = operatives
            .Where(ent =>
                ent.Item3.MapID == shuttleMapId
                || ent.Item3.MapID == targetStationMap)
            .Any(ent => ent.Item2.CurrentState == DamageState.Alive && ent.Item1.Running);

        if (operativesAlive)
            return; // There are living operatives than can access the shuttle, or are still on the station's map.

        // Check that there are spawns available and that they can access the shuttle.
        var spawnsAvailable = EntityQuery<NukeOperativeSpawnerComponent>(true).Any();
        if (spawnsAvailable && shuttleMapId == _nukiePlanet)
            return; // Ghost spawns can still access the shuttle. Continue the round.

        // The shuttle is inaccessible to both living nuke operatives and yet to spawn nuke operatives,
        // and there are no nuclear operatives on the target station's map.
        if (spawnsAvailable)
        {
            _winConditions.Add(WinCondition.NukiesAbandoned);
        }
        else
        {
            _winConditions.Add(WinCondition.AllNukiesDead);
        }

        RuleWinType = WinType.CrewMajor;
    }

    private void OnNukeDisarm(NukeDisarmSuccessEvent ev)
    {
        CheckRoundShouldEnd();
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

            if (_cfg.GetCVar(CCVars.WhitelistEnabled) && !player.ContentData()!.Whitelisted)
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
            var name = session.AttachedEntity == null
                ? string.Empty
                : MetaData(session.AttachedEntity.Value).EntityName;
            // TODO: Fix this being able to have duplicates
            _operativePlayers[name] = session;
        }
    }

    private void OnPlayersGhostSpawning(EntityUid uid, NukeOperativeComponent component, GhostRoleSpawnerUsedEvent args)
    {
        var spawner = args.Spawner;

        if (!TryComp<NukeOperativeSpawnerComponent>(spawner, out var nukeOpSpawner))
            return;

        HumanoidCharacterProfile? profile = null;
        if (TryComp(args.Spawned, out ActorComponent? actor))
            profile = _prefs.GetPreferences(actor.PlayerSession.UserId).SelectedCharacter as HumanoidCharacterProfile;

        SetupOperativeEntity(uid, nukeOpSpawner.OperativeName, nukeOpSpawner.OperativeStartingGear, profile);

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
        if (_operativePlayers.ContainsValue(playerSession))
            return;

        var name = MetaData(uid).EntityName;

        _operativePlayers.Add(name, playerSession);

        if (_ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (_nukeopsRuleConfig.GreetSound != null)
            _audioSystem.PlayGlobal(_nukeopsRuleConfig.GreetSound, playerSession);

        if (_targetStation != null && !string.IsNullOrEmpty(Name(_targetStation.Value)))
            _chatManager.DispatchServerMessage(playerSession, Loc.GetString("nukeops-welcome", ("station", _targetStation.Value)));
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
        var options = new MapLoadOptions()
        {
            LoadMap = true,
        };

        if (!_map.TryLoad(mapId, path.ToString(), out var outpostGrids, options) || outpostGrids.Count == 0)
        {
            Logger.ErrorS("nukies", $"Error loading map {path} for nukies!");
            return false;
        }

        // Assume the first grid is the outpost grid.
        _nukieOutpost = outpostGrids[0];

        // Listen I just don't want it to overlap.
        if (!_map.TryLoad(mapId, shuttlePath.ToString(), out var grids, new MapLoadOptions {Offset = Vector2.One*1000f}) || !grids.Any())
        {
            Logger.ErrorS("nukies", $"Error loading grid {shuttlePath} for nukies!");
            return false;
        }

        var shuttleId = grids.First();

        // Naughty, someone saved the shuttle as a map.
        if (Deleted(shuttleId))
        {
            Logger.ErrorS("nukeops", $"Tried to load nukeops shuttle as a map, aborting.");
            _mapManager.DeleteMap(mapId);
            return false;
        }

        if (TryComp<ShuttleComponent>(shuttleId, out var shuttle))
        {
            _shuttleSystem.TryFTLDock(shuttle, _nukieOutpost.Value);
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
                name = Loc.GetString("nukeops-role-commander") + " " + _random.PickAndTake(_operativeNames[_nukeopsRuleConfig.EliteNames]);
                role = _nukeopsRuleConfig.CommanderRolePrototype;
                gear = _nukeopsRuleConfig.CommanderStartGearPrototype;
                break;
            case 1:
                name = Loc.GetString("nukeops-role-agent") + " " + _random.PickAndTake(_operativeNames[_nukeopsRuleConfig.NormalNames]);
                role = _nukeopsRuleConfig.OperativeRoleProto;
                gear = _nukeopsRuleConfig.MedicStartGearPrototype;
                break;
            default:
                name = Loc.GetString("nukeops-role-operator") + " " + _random.PickAndTake(_operativeNames[_nukeopsRuleConfig.NormalNames]);
                role = _nukeopsRuleConfig.OperativeRoleProto;
                gear = _nukeopsRuleConfig.OperativeStartGearPrototype;
                break;
        }

        return (name, role, gear);
    }

    /// <summary>
    ///     Adds missing nuke operative components, equips starting gear and renames the entity.
    /// </summary>
    private void SetupOperativeEntity(EntityUid mob, string name, string gear, HumanoidCharacterProfile? profile)
    {
        MetaData(mob).EntityName = name;
        EntityManager.EnsureComponent<RandomHumanoidAppearanceComponent>(mob);
        EntityManager.EnsureComponent<NukeOperativeComponent>(mob);

        if(_startingGearPrototypes.TryGetValue(gear, out var gearPrototype))
            _stationSpawningSystem.EquipStartingGear(mob, gearPrototype, profile);

        _faction.RemoveFaction(mob, "NanoTrasen", false);
        _faction.AddFaction(mob, "Syndicate");
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
                var mob = _randomHumanoid.SpawnRandomHumanoid(_nukeopsRuleConfig.RandomHumanoidSettingsPrototype, _random.Pick(spawns), string.Empty);
                var profile = _prefs.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile;
                SetupOperativeEntity(mob, spawnDetails.Name, spawnDetails.Gear, profile);

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
                spawner.RoleName = Loc.GetString(nukeOpsAntag.Name);
                spawner.RoleDescription = Loc.GetString(nukeOpsAntag.Objective);

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
        SetOutfitCommand.SetOutfit(mind.OwnedEntity.Value, "SyndicateOperativeGearFull", EntityManager);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded || Configuration is not NukeopsRuleConfiguration nukeOpsConfig)
            return;

        _nukeopsRuleConfig = nukeOpsConfig;
        var minPlayers = nukeOpsConfig.MinPlayers;
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
        RuleWinType = WinType.Neutral;
        _winConditions.Clear();
        _nukieOutpost = null;
        _nukiePlanet = null;

        _startingGearPrototypes.Clear();
        _operativeNames.Clear();
        _operativeMindPendingData.Clear();
        _operativePlayers.Clear();

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
            if (mindComp.Mind == null || !mindComp.Mind.TryGetSession(out var session))
                continue;
            var name = MetaData(mindComp.Owner).EntityName;
            _operativePlayers.Add(name, session);
        }

        if (GameTicker.RunLevel == GameRunLevel.InRound)
            SpawnOperativesForGhostRoles();
    }

    public override void Ended() { }
}
