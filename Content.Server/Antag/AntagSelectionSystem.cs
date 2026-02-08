using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Antag.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost.Roles;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Antag;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Antag;

/// <summary>
/// Turns players into antags.
/// </summary>
/// <remarks>
/// Do not ever ever ever spawn and initialize an entity prototype in nullspace then move it to the grid.
/// I wasted 4 hours refactoring this system specifically to fix that mistake.
/// Always initialize your entities attached to the entity you're spawning them on, or the correct map at the very least.
/// </remarks>
public sealed partial class AntagSelectionSystem : GameRuleSystem<AntagSelectionComponent>
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IBanManager _ban = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly IServerPreferencesManager _pref = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ArrivalsSystem _arrivals = default!;

    // arbitrary random number to give late joining some mild interest.
    public const float LateJoinRandomChance = 0.5f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        Log.Level = LogLevel.Debug;

        SubscribeLocalEvent<GhostRoleAntagSpawnerComponent, TakeGhostRoleEvent>(OnTakeGhostRole);

        SubscribeLocalEvent<AntagSelectionComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);

        SubscribeLocalEvent<NoJobsAvailableSpawningEvent>(OnJobNotAssigned);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobsAssigned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
    }

    private void OnTakeGhostRole(Entity<GhostRoleAntagSpawnerComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.TookRole)
            return;

        if (ent.Comp.Rule is not { } rule || ent.Comp.Definition is not { } def)
            return;

        if (!Exists(rule) || !TryComp<AntagSelectionComponent>(rule, out var select))
            return;

        AttachSessionToAntagonist((rule, select), args.Player, def, _transform.GetMapCoordinates(ent));
        args.TookRole = true;
        _ghostRole.UnregisterGhostRole((ent, Comp<GhostRoleComponent>(ent)));
    }

    private void OnPlayerSpawning(RulePlayerSpawningEvent args)
    {
        var pool = args.PlayerPool;

        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            if (comp.SelectionTime != AntagSelectionTime.PrePlayerSpawn && comp.SelectionTime != AntagSelectionTime.IntraPlayerSpawn)
                continue;

            if (comp.AssignmentComplete)
                continue;

            ChooseAntags((uid, comp), pool); // We choose the antags here...

            if (comp.SelectionTime == AntagSelectionTime.PrePlayerSpawn)
            {
                AssignPreSelectedSessions((uid, comp)); // ...But only assign them if PrePlayerSpawn
                foreach (var session in comp.AssignedSessions)
                {
                    args.PlayerPool.Remove(session);
                    GameTicker.PlayerJoinGame(session);
                }
            }
        }

        // If IntraPlayerSpawn is selected, delayed rules should choose at this point too.
        var queryDelayed = QueryDelayedRules();
        while (queryDelayed.MoveNext(out var uid, out _, out var comp, out _))
        {
            if (comp.SelectionTime != AntagSelectionTime.IntraPlayerSpawn)
                continue;

            ChooseAntags((uid, comp), pool);
        }
    }

    private void OnJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            if (comp.SelectionTime != AntagSelectionTime.PostPlayerSpawn && comp.SelectionTime != AntagSelectionTime.IntraPlayerSpawn)
                continue;

            ChooseAntags((uid, comp), args.Players);
            AssignPreSelectedSessions((uid, comp));
        }
    }

    private void OnJobNotAssigned(NoJobsAvailableSpawningEvent args)
    {
        // If someone fails to spawn in due to there being no jobs, they should be removed from any preselected antags.
        // We only care about delayed rules, since if they're active the player should have already been removed via MakeAntag.
        var query = QueryDelayedRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            if (comp.SelectionTime != AntagSelectionTime.IntraPlayerSpawn)
                continue;

            if (!comp.RemoveUponFailedSpawn)
                continue;

            foreach (var def in comp.Definitions)
            {
                if (!comp.PreSelectedSessions.TryGetValue(def, out var session))
                    break;
                session.Remove(args.Player);
            }
        }
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!args.LateJoin)
            return;

        TryMakeLateJoinAntag(args.Player);
    }

    /// <summary>
    /// Attempt to make this player be a late-join antag.
    /// </summary>
    /// <param name="session">The session to attempt to make antag.</param>
    [PublicAPI]
    public bool TryMakeLateJoinAntag(ICommonSession session)
    {
        // TODO: this really doesn't handle multiple latejoin definitions well
        // eventually this should probably store the players per definition with some kind of unique identifier.
        // something to figure out later.

        var query = QueryAllRules();
        var rules = new List<(EntityUid, AntagSelectionComponent)>();
        while (query.MoveNext(out var uid, out var antag, out _))
        {
            if (HasComp<ActiveGameRuleComponent>(uid))
                rules.Add((uid, antag));
        }
        RobustRandom.Shuffle(rules);

        foreach (var (uid, antag) in rules)
        {
            if (!RobustRandom.Prob(LateJoinRandomChance))
                continue;

            if (!antag.Definitions.Any(p => p.LateJoinAdditional))
                continue;

            DebugTools.AssertNotEqual(antag.SelectionTime, AntagSelectionTime.PrePlayerSpawn);

            // do not count players in the lobby for the antag ratio
            var players = _playerManager.NetworkedSessions.Count(x => x.AttachedEntity != null);

            if (!TryGetNextAvailableDefinition((uid, antag), out var def, players))
                continue;

            if (TryMakeAntag((uid, antag), session, def.Value))
                return true;
        }

        return false;
    }

    protected override void Added(EntityUid uid, AntagSelectionComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        for (var i = 0; i < component.Definitions.Count; i++)
        {
            var def = component.Definitions[i];

            if (def.MinRange != null)
            {
                def.Min = def.MinRange.Value.Next(RobustRandom);
            }

            if (def.MaxRange != null)
            {
                def.Max = def.MaxRange.Value.Next(RobustRandom);
            }
        }
    }

    protected override void Started(EntityUid uid, AntagSelectionComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // If the round has not yet started, we defer antag selection until roundstart
        if (GameTicker.RunLevel != GameRunLevel.InRound)
            return;

        if (component.AssignmentComplete)
            return;

        var players = _playerManager.Sessions
            .Where(x => GameTicker.PlayerGameStatuses.TryGetValue(x.UserId, out var status) &&
                        status == PlayerGameStatus.JoinedGame)
            .ToList();

        ChooseAntags((uid, component), players, midround: true);
        AssignPreSelectedSessions((uid, component));
    }

    /// <summary>
    /// Chooses antagonists from the given selection of players
    /// </summary>
    /// <param name="ent">The antagonist rule entity</param>
    /// <param name="pool">The players to choose from</param>
    /// <param name="midround">Disable picking players for pre-spawn antags in the middle of a round</param>
    public void ChooseAntags(Entity<AntagSelectionComponent> ent, IList<ICommonSession> pool, bool midround = false)
    {
        foreach (var def in ent.Comp.Definitions)
        {
            ChooseAntags(ent, pool, def, midround: midround);
        }

        ent.Comp.PreSelectionsComplete = true;
    }

    /// <summary>
    /// Chooses antagonists from the given selection of players for the given antag definition.
    /// </summary>
    /// <param name="ent">The antagonist rule entity</param>
    /// <param name="pool">The players to choose from</param>
    /// <param name="def">The antagonist selection parameters and criteria</param>
    /// <param name="midround">Disable picking players for pre-spawn antags in the middle of a round</param>
    public void ChooseAntags(Entity<AntagSelectionComponent> ent,
        IList<ICommonSession> pool,
        AntagSelectionDefinition def,
        bool midround = false)
    {
        var playerPool = GetPlayerPool(ent, pool, def);
        var existingAntagCount = ent.Comp.PreSelectedSessions.TryGetValue(def, out var existingAntags) ? existingAntags.Count : 0;
        var count = GetTargetAntagCount(ent, GetTotalPlayerCount(pool), def) - existingAntagCount;

        // if there is both a spawner and players getting picked, let it fall back to a spawner.
        var noSpawner = def.SpawnerPrototype == null;
        var picking = def.PickPlayer;
        if (midround && ent.Comp.SelectionTime == AntagSelectionTime.PrePlayerSpawn)
        {
            // prevent antag selection from happening if the round is on-going, requiring a spawner if used midround.
            // this is so rules like nukies, if added by an admin midround, dont make random living people nukies
            Log.Info($"Antags for rule {ent:?} get picked pre-spawn so only spawners will be made.");
            DebugTools.Assert(def.SpawnerPrototype != null, $"Rule {ent:?} had no spawner for pre-spawn rule added mid-round!");
            picking = false;
        }

        for (var i = 0; i < count; i++)
        {
            var session = (ICommonSession?)null;
            if (picking)
            {
                if (!playerPool.TryPickAndTake(RobustRandom, out session) && noSpawner)
                {
                    Log.Warning($"Couldn't pick a player for {ToPrettyString(ent):rule}, no longer choosing antags for this definition");
                    break;
                }

                if (session != null && ent.Comp.PreSelectedSessions.Values.Any(x => x.Contains(session)))
                {
                    Log.Warning($"Somehow picked {session} for an antag when this rule already selected them previously");
                    continue;
                }
            }

            if (session == null)
                CreateAntagSpawner(ent, def); // Create a spawner since there's no session to attach.
            else
                PreSelectSessionForAntagonist(ent, session, def);
        }
    }

    /// <summary>
    /// Assigns antag roles to sessions selected for it.
    /// </summary>
    public void AssignPreSelectedSessions(Entity<AntagSelectionComponent> ent)
    {
        // Only assign if there's been a pre-selection, and the selection hasn't already been made
        if (!ent.Comp.PreSelectionsComplete || ent.Comp.AssignmentComplete)
            return;

        foreach (var def in ent.Comp.Definitions)
        {
            if (!ent.Comp.PreSelectedSessions.TryGetValue(def, out var set))
                continue;

            foreach (var session in set)
            {
                TryMakeAntag(ent, session, def);
            }
        }

        ent.Comp.AssignmentComplete = true;
    }

    /// <summary>
    /// Tries to makes a given player into the specified antagonist.
    /// </summary>
    public bool TryMakeAntag(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSelectionDefinition def, bool checkPref = true, bool onlyPreSelect = false)
    {
        _adminLogger.Add(LogType.AntagSelection, $"Start trying to make {session} become the antagonist: {ToPrettyString(ent)}");

        if (checkPref && !ValidAntagPreference(session, def.PrefRoles))
            return false;

        if (!IsSessionValid(ent, session, def) || !IsEntityValid(session.AttachedEntity, def))
            return false;

        if (onlyPreSelect)
            PreSelectSessionForAntagonist(ent, session, def);
        else
            MakeSessionAntagonist(ent, session, def);

        return true;
    }

    /// <summary>
    /// Create an antag spawner which can be taken over by a player through the ghost role system.
    /// </summary>
    /// <param name="ent">Antag rule entity</param>
    /// <param name="def">Antag selection definition chosen from the entity</param>
    [PublicAPI]
    private EntityUid? CreateAntagSpawner(Entity<AntagSelectionComponent> ent, AntagSelectionDefinition def)
    {
        if (def.SpawnerPrototype is not { } proto)
            return null;

        var spawner = Spawn(def.SpawnerPrototype);
        if (!TryValidSpawnPosition(ent, spawner))
        {
            Log.Error($"Found no valid positions to place antag spawner {ToPrettyString(spawner)} prototype: {proto}");
            Del(spawner);
            return null;
        }

        if (!TryComp<GhostRoleAntagSpawnerComponent>(spawner, out var spawnerComp))
        {
            Log.Error($"Antag spawner {spawner} does not have a {nameof(GhostRoleAntagSpawnerComponent)}.");
            _adminLogger.Add(LogType.AntagSelection, $"Antag spawner {spawner} in gamerule {ToPrettyString(ent)} failed due to not having {nameof(GhostRoleAntagSpawnerComponent)}.");
            Del(spawner);
            return null;
        }

        spawnerComp.Rule = ent;
        spawnerComp.Definition = def;
        return spawner;
    }

    /// <summary>
    /// Does antag pre-selection logic, adding a specified player session to the PreSelection list and logging it for admins.
    /// </summary>
    private void PreSelectSessionForAntagonist(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSelectionDefinition def)
    {
        if (!ent.Comp.PreSelectedSessions.TryGetValue(def, out var set))
            ent.Comp.PreSelectedSessions.Add(def, set = new HashSet<ICommonSession>());
        set.Add(session);

        Log.Debug($"Pre-selected {session.Name} as antagonist: {ToPrettyString(ent)}");
        _adminLogger.Add(LogType.AntagSelection, $"Pre-selected {session.Name} as antagonist: {ToPrettyString(ent)}");
    }

    /// <summary>
    /// Creates a new antagonist entity at the specified coordinates, then attaches the specified player to that antagonist.
    /// </summary>
    private EntityUid? AttachSessionToAntagonist(Entity<AntagSelectionComponent> ent,
        ICommonSession session,
        AntagSelectionDefinition def,
        MapCoordinates coords)
    {
        PreSelectSessionForAntagonist(ent, session, def);
        ent.Comp.AssignedSessions.Add(session);
        return SpawnNewAntagonist(ent, session, def, coords);
    }

    /// <summary>
    /// Makes a specified player into a specified antagonist.
    /// If the player is a ghost or has no attached entity, it will attempt to find a valid spawn position and spawn a new entity.
    /// Otherwise, it will try to move their current entity to their antag's spawn position (if it exists) and then set them up as antag.
    /// </summary>
    private EntityUid? MakeSessionAntagonist(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSelectionDefinition def)
    {
        PreSelectSessionForAntagonist(ent, session, def);

        ent.Comp.AssignedSessions.Add(session);

        // If the player has no entity to make an antagonist, make a new entity for them
        if (HasComp<GhostComponent>(session.AttachedEntity) || session.AttachedEntity is not { } player)
        {
            return SpawnNewAntagonist(ent, session, def);
        }

        TryValidSpawnPosition(ent, player, session);
        InitializeAntag(ent, player, session, def);
        return player;
    }

    /// <summary>
    /// Attempts to create a new antagonist entity and attach a player session to it at a valid spawnpoint.
    /// Does nothing if it cannot find a valid spawnpoint.
    /// </summary>
    private EntityUid? SpawnNewAntagonist(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSelectionDefinition def)
    {
        if (GetValidSpawnPosition(ent, session.AttachedEntity, session) is not { } coordinates)
        {
            Log.Error($"Was unable to find a valid spawn position for, {session.Name}, gamerule: {ToPrettyString(ent)} when trying to make them into an antagonist.");
            return null;
        }

        return SpawnNewAntagonist(ent, session, def, coordinates);
    }

    /// <summary>
    /// Attempts to create a new antagonist entity at the specified coordinates and attach a player session to it.
    /// If it cannot spawn an antagonist entity, it does nothing.
    /// </summary>
    private EntityUid? SpawnNewAntagonist(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSelectionDefinition def, MapCoordinates coordinates)
    {
        var getEntEv = new AntagSelectEntityEvent(session, ent, def.PrefRoles, coordinates);

        RaiseLocalEvent(ent, ref getEntEv, true);
        if (getEntEv.Entity is not { } antag)
        {
            Log.Error($"Tried to make {session.UserId} into an antagonist but was unable to spawn an entity for them. Gamerule {ToPrettyString(ent)}");
            return null;
        }

        InitializeAntag(ent, antag, session, def);
        return antag;
    }

    /// <summary>
    /// Raises an event to the gamerule to check all valid possible spawning points for this rule.
    /// Returns a random spawnpoint from a list of valid spawnpoints, or null if there weren't any.
    /// </summary>
    private MapCoordinates? GetValidSpawnPosition(Entity<AntagSelectionComponent> ent, EntityUid? antag, ICommonSession? session = null)
    {
        var getPosEv = new AntagSelectLocationEvent(ent, antag, session);
        RaiseLocalEvent(ent, ref getPosEv, true);

        if (!getPosEv.Handled)
            return null;

        return RobustRandom.Pick(getPosEv.Coordinates);
    }

    /// <summary>
    ///  Looks for a valid spawn position for this antagonist type, then moves the antagonist entity to that spawn position.
    /// </summary>
    private bool TryValidSpawnPosition(Entity<AntagSelectionComponent> ent, EntityUid antag, ICommonSession? session = null)
    {
        if (GetValidSpawnPosition(ent, antag, session) is not { } coordinates)
            return false;

        var xform = Transform(antag);
        _transform.SetMapCoordinates((antag, xform), coordinates);
        return true;
    }

    /// <summary>
    /// Initializes the antagonist status on the specified entity.
    /// Adds the needed components, loadouts, items, attaches the player and fires off an event.
    /// </summary>
    private void InitializeAntag(Entity<AntagSelectionComponent> ent, EntityUid antag, ICommonSession? session, AntagSelectionDefinition def)
    {
        // The following is where we apply components, equipment, and other changes to our antagonist entity.
        EntityManager.AddComponents(antag, def.Components);

        // Equip the entity's RoleLoadout and LoadoutGroup
        List<ProtoId<StartingGearPrototype>> gear = new();
        if (def.StartingGear is not null)
            gear.Add(def.StartingGear.Value);

        _loadout.Equip(antag, gear, def.RoleLoadout);

        if (session != null)
        {
            var curMind = session.GetMind();

            if (curMind == null ||
                !TryComp<MindComponent>(curMind.Value, out var mindComp) ||
                mindComp.OwnedEntity != antag)
            {
                curMind = _mind.CreateMind(session.UserId, Name(antag));
                _mind.SetUserId(curMind.Value, session.UserId);
            }

            _mind.TransferTo(curMind.Value, antag, ghostCheckOverride: true);
            _role.MindAddRoles(curMind.Value, def.MindRoles, null, true);
            ent.Comp.AssignedMinds.Add((curMind.Value, Name(antag)));
            SendBriefing(session, def.Briefing);

            Log.Debug($"Assigned {ToPrettyString(curMind)} as antagonist: {ToPrettyString(ent)}");
            _adminLogger.Add(LogType.AntagSelection, $"Assigned {ToPrettyString(curMind)} as antagonist: {ToPrettyString(ent)}");
        }

        var afterEv = new AfterAntagEntitySelectedEvent(session, antag, ent, def);
        RaiseLocalEvent(ent, ref afterEv, true);
    }

    /// <summary>
    /// Gets an ordered player pool based on player preferences and the antagonist definition.
    /// </summary>
    public AntagSelectionPlayerPool GetPlayerPool(Entity<AntagSelectionComponent> ent, IList<ICommonSession> sessions, AntagSelectionDefinition def)
    {
        var preferredList = new List<ICommonSession>();
        var fallbackList = new List<ICommonSession>();
        foreach (var session in sessions)
        {
            if (!IsSessionValid(ent, session, def) || !IsEntityValid(session.AttachedEntity, def))
                continue;

            if (ent.Comp.PreSelectedSessions.TryGetValue(def, out var preSelected) && preSelected.Contains(session))
                continue;

            // Add player to the appropriate antag pool
            if (ValidAntagPreference(session, def.PrefRoles))
            {
                preferredList.Add(session);
            }
            else if (ValidAntagPreference(session, def.FallbackRoles))
            {
                fallbackList.Add(session);
            }
        }

        return new AntagSelectionPlayerPool(new() { preferredList, fallbackList });
    }

    /// <summary>
    /// Checks if a given session is valid for an antagonist.
    /// </summary>
    public bool IsSessionValid(Entity<AntagSelectionComponent> ent, ICommonSession? session, AntagSelectionDefinition def, EntityUid? mind = null)
    {
        // TODO ROLE TIMERS
        // Check if antag role requirements are met

        if (session == null)
            return true;

        if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
            return false;

        if (ent.Comp.AssignedSessions.Contains(session))
            return false;

        mind ??= session.GetMind();

        //todo: we need some way to check that we're not getting the same role twice. (double picking thieves or zombies through midrounds)

        switch (def.MultiAntagSetting)
        {
            case AntagAcceptability.None:
                {
                    if (_role.MindIsAntagonist(mind))
                        return false;
                    if (GetPreSelectedAntagSessions(def).Contains(session)) // Used for rules where the antag has been selected, but not started yet
                        return false;
                    break;
                }
            case AntagAcceptability.NotExclusive:
                {
                    if (_role.MindIsExclusiveAntagonist(mind))
                        return false;
                    if (GetPreSelectedExclusiveAntagSessions(def).Contains(session))
                        return false;
                    break;
                }
        }

        // todo: expand this to allow for more fine antag-selection logic for game rules.
        if (!_jobs.CanBeAntag(session))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if a given entity (mind/session not included) is valid for a given antagonist.
    /// </summary>
    public bool IsEntityValid(EntityUid? entity, AntagSelectionDefinition def)
    {
        // If the player has not spawned in as any entity (e.g., in the lobby), they can be given an antag role/entity.
        if (entity == null)
            return true;

        if (_arrivals.IsOnArrivals((entity.Value, null)))
            return false;

        if (!def.AllowNonHumans && !HasComp<HumanoidProfileComponent>(entity))
            return false;

        if (def.Whitelist != null)
        {
            if (!_whitelist.IsValid(def.Whitelist, entity.Value))
                return false;
        }

        if (def.Blacklist != null)
        {
            if (_whitelist.IsValid(def.Blacklist, entity.Value))
                return false;
        }

        return true;
    }

    private void OnObjectivesTextGetInfo(Entity<AntagSelectionComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        if (ent.Comp.AgentName is not { } name)
            return;

        args.Minds = ent.Comp.AssignedMinds;
        args.AgentName = Loc.GetString(name);
    }
}

/// <summary>
/// Event raised on a game rule entity in order to determine what the antagonist entity will be.
/// Only raised if the selected player's current entity is invalid.
/// </summary>
/// TODO: This should really be an interface instead, we're always raising this to the same entity anyways and the values are extremely predictable
[ByRefEvent]
public record struct AntagSelectEntityEvent(ICommonSession? Session, Entity<AntagSelectionComponent> GameRule, List<ProtoId<AntagPrototype>> AntagRoles, MapCoordinates Coords)
{
    public readonly ICommonSession? Session = Session;

    /// list of antag role prototypes associated with a entity. used by the <see cref="AntagMultipleRoleSpawnerComponent"/>
    public readonly List<ProtoId<AntagPrototype>> AntagRoles = AntagRoles;

    public readonly MapCoordinates Coords = Coords;

    public bool Handled => Entity != null;

    public EntityUid? Entity;
}

/// <summary>
/// Event raised on a game rule entity to determine the location for the antagonist.
/// </summary>
[ByRefEvent]
public record struct AntagSelectLocationEvent(Entity<AntagSelectionComponent> GameRule, EntityUid? Entity, ICommonSession? Session = null)
{
    public readonly ICommonSession? Session = Session;

    public bool Handled => Coordinates.Any();

    // the entity of the antagonist
    public EntityUid? Entity = Entity;

    public List<MapCoordinates> Coordinates = new();
}

/// <summary>
/// Event raised on a game ruleR entity after the setup logic for an antag is complete.
/// Used for applying additional more complex setup logic.
/// </summary>
[ByRefEvent]
public readonly record struct AfterAntagEntitySelectedEvent(ICommonSession? Session, EntityUid EntityUid, Entity<AntagSelectionComponent> GameRule, AntagSelectionDefinition Def);
