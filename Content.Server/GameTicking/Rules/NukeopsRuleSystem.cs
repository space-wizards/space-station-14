using System.Linq;
using Content.Server.CharacterAppearance.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules.Prototypes;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind.Components;
using Content.Server.Nuke;
using Content.Server.Players;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.MobState;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.Traitor;
using Content.Shared.Preferences;
using Robust.Shared.Network;
using Job = Content.Server.Roles.Job;

namespace Content.Server.GameTicking.Rules;

public sealed class NukeopsRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapLoader _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IPlayerManager _playerSystem = default!;

    private Dictionary<EntityUid, (Mind.Mind? mind, bool alive)> _aliveNukeops = new();
    private bool _opsWon;

    private MapId? _nukiePlanet;
    private EntityUid? _nukieOutpost;

    public override string Prototype => "Nukeops";

    private NukeopsRuleConfigPrototype _nukeopsRuleConfig = new();

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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<NukeExplodedEvent>(OnNukeExploded);
        SubscribeLocalEvent<NukeOperativeComponent, GhostRoleSpawnerUsedEvent>(OnPlayersGhostSpawning);
        SubscribeLocalEvent<NukeOperativeComponent, MindAddedMessage>(OnMindAdded);
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
        foreach (var nukeop in _aliveNukeops)
        {
            ev.AddLine($"- {nukeop.Value.mind?.Session?.Name}");
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!RuleAdded)
            return;

        if (!_aliveNukeops.TryFirstOrNull(x => x.Value.mind?.OwnedEntity == ev.Entity, out var op))
            return;

        var state = op.Value.Value;
        var alive = state.mind?.CharacterDeadIC ?? ev.CurrentMobState is DamageState.Critical or DamageState.Dead;

        _aliveNukeops[ev.Entity] = (state.mind, alive);

        if (_aliveNukeops.Values.All(x => x.mind != null && !x.alive))
        {
            _roundEndSystem.EndRound();
        }
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        if (!RuleAdded)
            return;

        _aliveNukeops.Clear();

        // Basically copied verbatim from traitor code
        var playersPerOperative = _cfg.GetCVar(CCVars.NukeopsPlayersPerOp);
        var maxOperatives = _cfg.GetCVar(CCVars.NukeopsMaxOps);

        var everyone = new List<IPlayerSession>(ev.PlayerPool);
        var prefList = new List<IPlayerSession>();
        var cmdrPrefList = new List<IPlayerSession>();
        var operatives = new List<IPlayerSession>();

        // The LINQ expression ReSharper keeps suggesting is completely unintelligible so I'm disabling it
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var player in everyone)
        {
            if(player.ContentData()?.Mind?.AllRoles.All(role => role is not Job {CanBeAntag: false}) ?? false) continue;
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
        }
    }

    private void OnPlayersGhostSpawning(EntityUid uid, NukeOperativeComponent component, GhostRoleSpawnerUsedEvent args)
    {
        var spawner = args.Spawner;

        if (!TryComp<NukeOperativeSpawnerComponent>(spawner, out var nukeOpSpawner))
            return;

        SetupOperativeEntity(uid, nukeOpSpawner.OperativeName, nukeOpSpawner.OperativeStartingGear);

        _aliveNukeops.Add(uid, (null, true));
        _operativeMindPendingData.Add(uid, nukeOpSpawner.OperativeRolePrototype);
    }

    private void OnMindAdded(EntityUid uid, NukeOperativeComponent component, MindAddedMessage args)
    {
        if (!_aliveNukeops.ContainsKey(uid) || !TryComp<MindComponent>(uid, out var mindComponent))
            return;

        if (mindComponent.Mind == null)
            return;

        var mind = mindComponent.Mind;

        _aliveNukeops[uid] = (mind, mind.CharacterDeadIC);
        if (_operativeMindPendingData.TryGetValue(uid, out var role))
        {
            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(role)));
            _operativeMindPendingData.Remove(uid);
        }
    }

    private bool SpawnMap()
    {
        if (_nukiePlanet != null)
            return true; // Map is already loaded.

        var path = _nukeopsRuleConfig.NukieOutpostMap;
        var shuttlePath = _nukeopsRuleConfig.NukieShuttleMap;
        var mapId = _mapManager.CreateMap();

        var (_, outpost) = _mapLoader.LoadBlueprint(mapId, path);
        if (outpost == null)
        {
            Logger.ErrorS("nukies", $"Error loading map {path} for nukies!");
            return false;
        }

        // Listen I just don't want it to overlap.
        var (_, shuttleId) = _mapLoader.LoadBlueprint(mapId, shuttlePath, new MapLoadOptions()
        {
            Offset = Vector2.One * 1000f,
        });

        if (TryComp<ShuttleComponent>(shuttleId, out var shuttle))
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShuttleSystem>().TryFTLDock(shuttle, outpost.Value);
        }

        _nukiePlanet = mapId;
        _nukieOutpost = outpost;

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
                name = $"Commander " + _random.PickAndTake<string>(_operativeNames[_nukeopsRuleConfig.EliteNames]);
                role = _nukeopsRuleConfig.CommanderRolePrototype;
                gear = _nukeopsRuleConfig.CommanderStartGearPrototype;
                break;
            case 1:
                name = $"Agent " + _random.PickAndTake<string>(_operativeNames[_nukeopsRuleConfig.NormalNames]);
                role = _nukeopsRuleConfig.OperativeRoleProto;
                gear = _nukeopsRuleConfig.MedicStartGearPrototype;
                break;
            default:
                name = $"Operator " + _random.PickAndTake<string>(_operativeNames[_nukeopsRuleConfig.NormalNames]);
                role = _nukeopsRuleConfig.OperativeRoleProto;
                gear = _nukeopsRuleConfig.OperativeStartGearPrototype;
                break;
        }

        return (name, role, gear);
    }

    /// <summary>
    ///     Adds missing nuke operative components, equips starting gear and rename.
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

            if (_nukieOutpost != null && xform.ParentUid == _nukieOutpost)
            {
                spawns.Add(xform.Coordinates);
                break;
            }
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
                _aliveNukeops.Add(mob, (newMind, true));
            } else if (addSpawnPoints)
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

    private void SpawnOperativesForSpawningPlayers(List<IPlayerSession> playerPool, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {

    }

    private void SpawnOperativesForGhostRoles()
    {
        _aliveNukeops.Clear();

        // Basically copied verbatim from traitor code
        var playersPerOperative = _cfg.GetCVar(CCVars.NukeopsPlayersPerOp);
        var maxOperatives = _cfg.GetCVar(CCVars.NukeopsMaxOps);

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

        var minPlayers = _cfg.GetCVar(CCVars.NukeopsMinPlayers);
        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("nukeops-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("nukeops-no-one-ready"));
            ev.Cancel();
            return;
        }
    }

    public override void Started()
    {
        _opsWon = false;
        _nukieOutpost = null;
        _nukiePlanet = null;

        _startingGearPrototypes.Clear();
        _operativeNames.Clear();
        _operativeMindPendingData.Clear();

        var configPrototype = _cfg.GetCVar(CCVars.NukeOpsConfigProto);
        if (configPrototype.Length != 0)
            _nukeopsRuleConfig = _prototypeManager.Index<NukeopsRuleConfigPrototype>(configPrototype);

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

        if (GameTicker.RunLevel == GameRunLevel.InRound)
            SpawnOperativesForGhostRoles();
    }

    public override void Ended() { }
}
