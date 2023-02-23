using System.Linq;
using Content.Server.CharacterAppearance.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Humanoid.Systems;
using Content.Server.Mind.Components;
using Content.Server.NPC.Systems;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Traitor;
using Content.Shared.Dataset;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class WizardRuleSystem : GameRuleSystem
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
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;

    private MapId? _wizardMap;

    // TODO: use components, don't just cache entity UIDs. Until comp.owner has a replacement im not sure how to replace them
    private EntityUid? _wizardShuttle;
    private EntityUid? _targetStation;

    public override string Prototype => "Wizard";

    private WizardRuleConfiguration _wizardRuleConfig = new();

    private enum WinType
    {
        /// <summary>
        ///     Wizards died.
        /// </summary>
        WizardsEliminated,
        /// <summary>
        ///     Neutral win. The crew escaped, but the wizards did too.
        /// </summary>
        Neutral,
        //TODO: add more objectives and win conditions?
    }

    private WinType _winType = WinType.Neutral;

    private WinType RuleWinType
    {
        get => _winType;
        set
        {
            _winType = value;

            if (_wizardRuleConfig.EndsRound && value == WinType.WizardsEliminated)
            {
                _roundEndSystem.EndRound();
            }
        }
    }

    /// <summary>
    ///     Cached starting gear prototypes.
    /// </summary>
    private readonly Dictionary<string, StartingGearPrototype> _startingGearPrototypes = new ();

    /// <summary>
    ///     Cached wizard name prototypes.
    /// </summary>
    private readonly Dictionary<string, List<string>> _wizardNames = new();

    /// <summary>
    ///     Data to be used in <see cref="OnMindAdded"/> for a wizard once the Mind has been added.
    /// </summary>
    private readonly Dictionary<EntityUid, string> _wizardMindPendingData = new();

    /// <summary>
    ///     Players who played as a wizard at some point in the round.
    ///     Stores the session as well as the entity name
    /// </summary>
    private readonly Dictionary<string, IPlayerSession> _wizardPlayers = new();


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        SubscribeLocalEvent<WizardComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<WizardComponent, GhostRoleSpawnerUsedEvent>(OnPlayersGhostSpawning);
        SubscribeLocalEvent<WizardComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<WizardComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<WizardComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, WizardComponent component, ComponentInit args)
    {
        // If entity has a prior mind attached, add them to the players list.
        if (!TryComp<MindComponent>(uid, out var mindComponent) || !RuleAdded)
            return;

        var session = mindComponent.Mind?.Session;
        var name = MetaData(uid).EntityName;
        if (session != null)
            _wizardPlayers.Add(name, session);
    }

    private void OnComponentRemove(EntityUid uid, WizardComponent component, ComponentRemove args)
    {
        CheckRoundShouldEnd();
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

        foreach (var wizard in EntityQuery<WizardComponent>())
        {
            if (!TryComp<ActorComponent>(wizard.Owner, out var actor))
            {
                continue;
            }

            _chatManager.DispatchServerMessage(actor.PlayerSession, Loc.GetString("wizard-welcome"));
        }
    }

    private void OnRoundEnd()
    {
        _targetStation = null;
        _wizardMap = null;
        _wizardShuttle = null;
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        switch (_winType)
        {
            case WinType.WizardsEliminated:
                ev.AddLine(Loc.GetString("wizard-eliminated"));
                break;
            case WinType.Neutral:
                ev.AddLine(Loc.GetString("wizard-escaped"));
                break;
        }

        ev.AddLine(Loc.GetString("wizard-list-start"));
        foreach (var (name, session) in _wizardPlayers)
        {
            var listing = Loc.GetString("wizard-list-name", ("name", name), ("user", session.Name));
            ev.AddLine(listing);
        }
    }

    private void CheckRoundShouldEnd()
    {
        if (!RuleAdded)
            return;

        MapId? shuttleMapId = EntityManager.EntityExists(_wizardShuttle)
            ? Transform(_wizardShuttle!.Value).MapID
            : null;

        MapId? targetStationMap = null;
        if (_targetStation != null && TryComp(_targetStation, out StationDataComponent? data))
        {
            var grid = data.Grids.FirstOrNull();
            targetStationMap = grid != null
                ? Transform(grid.Value).MapID
                : null;
        }

        // Check if there are wizards still alive on the same map as the shuttle,
        // or on the same map as the station.
        // If there are, the round can continue.
        var wizards = EntityQuery<WizardComponent, MobStateComponent, TransformComponent>(true);
        var wizardsAlive = wizards
            .Where(ent =>
                ent.Item3.MapID == shuttleMapId
                || ent.Item3.MapID == targetStationMap)
            .Any(ent => ent.Item2.CurrentState == MobState.Alive && ent.Item1.Running);

        if (wizardsAlive)
            return; // There are living wizards can access the shuttle, or are still on the station's map.

        // Check that there are spawns available and that they can access the shuttle.
        var spawnsAvailable = EntityQuery<WizardSpawnerComponent>(true).Any();
        if (spawnsAvailable && shuttleMapId == _wizardMap)
            return; // Ghost spawns can still access the shuttle. Continue the round.

        RuleWinType = WinType.WizardsEliminated;
    }

    private void OnMobStateChanged(EntityUid uid, WizardComponent component, MobStateChangedEvent ev)
    {
        if(ev.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        if (!RuleAdded)
            return;

        // Basically copied verbatim from traitor code
        var playersPerWizard = _wizardRuleConfig.PlayersPerWizard;
        var maxWizards = _wizardRuleConfig.MaxWizards;

        var everyone = new List<IPlayerSession>(ev.PlayerPool);
        var prefList = new List<IPlayerSession>();
        var wizards = new List<IPlayerSession>();

        // The LINQ expression ReSharper keeps suggesting is completely unintelligible so I'm disabling it
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var player in everyone)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
            {
                continue;
            }
            var profile = ev.Profiles[player.UserId];
            if (profile.AntagPreferences.Contains(_wizardRuleConfig.WizardRoleProto))
            {
                prefList.Add(player);
            }
        }

        var numWizards = MathHelper.Clamp(ev.PlayerPool.Count / playersPerWizard, 1, maxWizards);

        for (var i = 0; i < numWizards; i++)
        {
            IPlayerSession wizard;

            if (prefList.Count == 0)
            {
                if (everyone.Count == 0)
                {
                    Logger.InfoS("preset", "Insufficient ready players to spawn wizards, stopping the selection");
                    break;
                }
                wizard = _random.PickAndTake(everyone);
                Logger.InfoS("preset", "Insufficient preferred wizards, picking at random.");
            }
            else
            {
                wizard = _random.PickAndTake(prefList);
                everyone.Remove(wizard);
                Logger.InfoS("preset", "Selected a preferred wizard.");
            }

            wizards.Add(wizard);
        }

        SpawnWizards(numWizards, wizards, false);

        foreach(var session in wizards)
        {
            ev.PlayerPool.Remove(session);
            GameTicker.PlayerJoinGame(session);
            var name = session.AttachedEntity == null
                ? string.Empty
                : MetaData(session.AttachedEntity.Value).EntityName;
            // TODO: Fix this being able to have duplicates
            _wizardPlayers[name] = session;
        }
    }

    private void OnPlayersGhostSpawning(EntityUid uid, WizardComponent component, GhostRoleSpawnerUsedEvent args)
    {
        var spawner = args.Spawner;

        if (!TryComp<WizardSpawnerComponent>(spawner, out var wizardSpawner))
            return;

        HumanoidCharacterProfile? profile = null;
        if (TryComp(args.Spawned, out ActorComponent? actor))
            profile = _prefs.GetPreferences(actor.PlayerSession.UserId).SelectedCharacter as HumanoidCharacterProfile;

        SetupWizardEntity(uid, wizardSpawner.WizardName, wizardSpawner.WizardStartingGear, profile);

        _wizardMindPendingData.Add(uid, wizardSpawner.WizardRolePrototype);
    }

    private void OnMindAdded(EntityUid uid, WizardComponent component, MindAddedMessage args)
    {
        if (!TryComp<MindComponent>(uid, out var mindComponent) || mindComponent.Mind == null)
            return;

        var mind = mindComponent.Mind;

        if (_wizardMindPendingData.TryGetValue(uid, out var role))
        {
            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(role)));
            _wizardMindPendingData.Remove(uid);
        }

        if (!mind.TryGetSession(out var playerSession))
            return;
        if (_wizardPlayers.ContainsValue(playerSession))
            return;

        var name = MetaData(uid).EntityName;

        _wizardPlayers.Add(name, playerSession);

        if (_ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (_targetStation != null && !string.IsNullOrEmpty(Name(_targetStation.Value)))
            _chatManager.DispatchServerMessage(playerSession, Loc.GetString("wizard-welcome"));
    }

    private bool SpawnMap()
    {
        if (_wizardMap != null)
            return true; // Map is already loaded.

        var shuttlePath = _wizardRuleConfig.WizardShuttleMap;

        if (shuttlePath == null)
        {
            Logger.ErrorS("wizards", "No shuttle map specified for wizards!");
            return false;
        }

        var mapId = _mapManager.CreateMap();

        if (!_map.TryLoad(mapId, shuttlePath.ToString(), out var grids, new MapLoadOptions {Offset = Vector2.One*1000f}) || !grids.Any())
        {
            Logger.ErrorS("wizards", $"Error loading grid {shuttlePath} for wizards!");
            return false;
        }

        var shuttleId = grids[0];

        // Naughty, someone saved the shuttle as a map.
        if (Deleted(shuttleId))
        {
            Logger.ErrorS("wizards", $"Tried to load wizard shuttle as a map, aborting.");
            _mapManager.DeleteMap(mapId);
            return false;
        }

        _wizardMap = mapId;
        _wizardShuttle = shuttleId;

        return true;
    }

    private (string Name, string Role, string Gear) GetWizardSpawnDetails(int spawnNumber)
    {
        string name;
        string role;
        string gear;

        //TODO: setup wizard and apprentice info in the case of multi-wizard, similar to NukeOps commander and operatives
        switch (spawnNumber)
        {
            default:
                name = _random.PickAndTake(_wizardNames[_wizardRuleConfig.WizardFirstNames]) +
                    " " +
                    _random.PickAndTake(_wizardNames[_wizardRuleConfig.WizardLastNames]);

                role = _wizardRuleConfig.WizardRoleProto;
                gear = _wizardRuleConfig.WizardStartGearPrototype;
                break;
        }

        return (name, role, gear);
    }

    /// <summary>
    ///     Adds wizard components, equips starting gear and renames the entity.
    /// </summary>
    private void SetupWizardEntity(EntityUid mob, string name, string gear, HumanoidCharacterProfile? profile)
    {
        MetaData(mob).EntityName = name;
        EntityManager.EnsureComponent<RandomHumanoidAppearanceComponent>(mob);
        EntityManager.EnsureComponent<WizardComponent>(mob);

        if(_startingGearPrototypes.TryGetValue(gear, out var gearPrototype))
            _stationSpawningSystem.EquipStartingGear(mob, gearPrototype, profile);

        _faction.RemoveFaction(mob, "NanoTrasen", false);
        _faction.AddFaction(mob, "Syndicate");
    }

    private void SpawnWizards(int spawnCount, List<IPlayerSession> sessions, bool addSpawnPoints)
    {
        if (_wizardShuttle == null)
            return;

        var spawns = new List<EntityCoordinates>();

        foreach (var (_, meta, xform) in EntityManager.EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != _wizardRuleConfig.SpawnPointPrototype)
                continue;

            if (xform.ParentUid != _wizardShuttle)
                continue;

            spawns.Add(xform.Coordinates);
            break;
        }

        if (spawns.Count == 0)
        {
            spawns.Add(EntityManager.GetComponent<TransformComponent>((EntityUid) _wizardShuttle).Coordinates);
            Logger.WarningS("wizards", $"Fell back to default spawn for wizards!");
        }

        // TODO: This should spawn the wizards in regardless and transfer if possible; rest should go to shot roles.
        for(var i = 0; i < spawnCount; i++)
        {
            var spawnDetails = GetWizardSpawnDetails(i);
            var wizardAntag = _prototypeManager.Index<AntagPrototype>(spawnDetails.Role);

            if (sessions.TryGetValue(i, out var session))
            {
                var mob = _randomHumanoid.SpawnRandomHumanoid(_wizardRuleConfig.RandomHumanoidSettingsPrototype, _random.Pick(spawns), string.Empty);
                var profile = _prefs.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile;
                SetupWizardEntity(mob, spawnDetails.Name, spawnDetails.Gear, profile);

                var newMind = new Mind.Mind(session.UserId)
                {
                    CharacterName = spawnDetails.Name
                };
                newMind.ChangeOwningPlayer(session.UserId);
                newMind.AddRole(new TraitorRole(newMind, wizardAntag));

                newMind.TransferTo(mob);
            }
            else if (addSpawnPoints)
            {
                var spawnPoint = EntityManager.SpawnEntity(_wizardRuleConfig.GhostSpawnPointProto, _random.Pick(spawns));
                var spawner = EnsureComp<GhostRoleMobSpawnerComponent>(spawnPoint);
                spawner.RoleName = Loc.GetString(wizardAntag.Name);
                spawner.RoleDescription = Loc.GetString(wizardAntag.Objective);

                var wizardSpawner = EnsureComp<WizardSpawnerComponent>(spawnPoint);
                wizardSpawner.WizardName = spawnDetails.Name;
                wizardSpawner.WizardRolePrototype = spawnDetails.Role;
                wizardSpawner.WizardStartingGear = spawnDetails.Gear;
            }
        }
    }

    private void SpawnWizardsForGhostRoles()
    {
        // Basically copied verbatim from traitor code
        var playersPerWizard = _wizardRuleConfig.PlayersPerWizard;
        var maxWizards = _wizardRuleConfig.MaxWizards;

        var playerPool = _playerSystem.ServerSessions.ToList();
        var numWizards = MathHelper.Clamp(playerPool.Count / playersPerWizard, 1, maxWizards);

        var wizards = new List<IPlayerSession>();
        SpawnWizards(numWizards, wizards, true);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded || Configuration is not WizardRuleConfiguration wizardConfig)
            return;

        _wizardRuleConfig = wizardConfig;
        var minPlayers = wizardConfig.MinPlayers;
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("wizard-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length != 0)
            return;

        _chatManager.DispatchServerAnnouncement(Loc.GetString("wizard-no-one-ready"));
        ev.Cancel();
    }

    public override void Started()
    {
        RuleWinType = WinType.Neutral;
        _wizardMap = null;

        _startingGearPrototypes.Clear();
        _wizardNames.Clear();
        _wizardMindPendingData.Clear();
        _wizardPlayers.Clear();

        var proto = _wizardRuleConfig.WizardStartGearPrototype;
        _startingGearPrototypes.Add(proto, _prototypeManager.Index<StartingGearPrototype>(proto));

        foreach (var name in new[] { _wizardRuleConfig.WizardFirstNames, _wizardRuleConfig.WizardLastNames })
        {
            _wizardNames.Add(name, new List<string>(_prototypeManager.Index<DatasetPrototype>(name).Values));
        }

        if (!SpawnMap())
        {
            Logger.InfoS("wizards", "Failed to load map for wizards");
            return;
        }

        // Add pre-existing wizards to the credit list.
        var query = EntityQuery<WizardComponent, MindComponent>(true);
        foreach (var (_, mindComp) in query)
        {
            if (mindComp.Mind == null || !mindComp.Mind.TryGetSession(out var session))
                continue;
            var name = MetaData(mindComp.Owner).EntityName;
            _wizardPlayers.Add(name, session);
        }

        if (GameTicker.RunLevel == GameRunLevel.InRound)
            SpawnWizardsForGhostRoles();
    }

    public override void Ended()
    {
    }
}
