using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Antag.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
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
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Server.Player;
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
    [Dependency] private readonly IBanManager _ban = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _pref = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ArrivalsSystem _arrivals = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

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

        if (ent.Comp.Rule is not { } rule || ent.Comp.Definition is not { } proto)
            return;

        if (!Proto.Resolve(proto, out var def))
            return;

        if (!Exists(rule) || !TryComp<AntagSelectionComponent>(rule, out var select))
            return;

        AttachSessionToAntagonist((rule, select), args.Player, def, _transform.GetMapCoordinates(ent));
        args.TookRole = true;
        _ghostRole.UnregisterGhostRole((ent, Comp<GhostRoleComponent>(ent)));
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!args.LateJoin)
            return;

        TryMakeLateJoinAntag(args.Player);
    }

    // This is called when the round starts, before jobs are selected
    // TODO: Remove the old code
    private void OnPlayerSpawning(RulePlayerSpawningEvent args)
    {
        var pool = args.PlayerPool;

        // Get all GameRules and store all antags from them in a list.
        List<AntagRule> definitions = [];
        var rulesQuery = QueryAllRules();
        while (rulesQuery.MoveNext(out var uid, out var antag, out _))
        {
            AddGameRuleDefinitions((uid, antag), pool, ref definitions);
        }

        // Pick a random player session and then try to assign the currently available antags from it!
        // This means each player has the same chance at rolling antag, with minimal alterations to the odds by number of antags selected.
        var weightedPool = GetWeightedPlayerPool(args.PlayerPool);
        if (!TryTicketAntags(weightedPool, ref definitions))
        {
            // TODO: Handle not all antags being assigned!
        }

        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            if (comp.SelectionTime == AntagSelectionTime.PostPlayerSpawn)
                continue;

            if (comp.AssignmentComplete)
                continue;

            ChooseAntags((uid, comp), pool); // We choose the antags here...

            if (comp.SelectionTime == AntagSelectionTime.PrePlayerSpawn)
            {
                AssignPreSelectedSessions((uid, comp)); // ...But only assign them if PrePlayerSpawn
                foreach (var session in comp.AssignedSessions)
                {
                    //args.PlayerPool.Remove(session);
                    //GameTicker.PlayerJoinGame(session);
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

    // TODO: This should handle "PostSpawn" antags, and we shouldn't need to query ActiveGameRules a *second* time...
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

    // TODO: This is probably fine? Double check.
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

            foreach (var def in comp.Antags)
            {
                if (!comp.PreSelectedSessions.TryGetValue(def, out var session))
                    break;
                session.Remove(args.Player);
            }
        }
    }

    private void AddGameRuleDefinitions(Entity<AntagSelectionComponent> gameRule, List<ICommonSession> sessions, ref List<AntagRule> roles)
    {
        AddGameRuleDefinitions(gameRule, sessions.Count, ref roles);
    }

    private void AddGameRuleDefinitions(Entity<AntagSelectionComponent> gameRule, int playerCount, ref List<AntagRule> roles)
    {
        var runningCount = 0;

        // We assume that antag definitions are prioritized by order, and take up slots that other roles may take.
        // I.E for Nukies, it selects 1 commander which takes up 10 players, then one corpsman which takes up another 10, then we select X nukies based on the remaining player count.
        // This is how the system worked when I got here, and I decided not to change it to avoid fucking with team antag balance
        foreach (var proto in gameRule.Comp.Antags)
        {
            if (!Proto.Resolve(proto, out var definition))
                continue;

            roles.Add((gameRule, definition, GetTargetAntagCount(definition, playerCount, ref runningCount)));
        }
    }

    private Dictionary<ICommonSession, float> GetWeightedPlayerPool(List<ICommonSession> sessions)
    {
        var dict = new Dictionary<ICommonSession, float>(sessions.Count);
        foreach (var session in sessions)
        {
            // TODO: Maybe we should also filter out sessions here instead? Haven't found a good reason for that yet though :P.
            // TODO: Actually add weights! This is placeholder for a future PR.
            dict.Add(session, 1f);
        }

        return dict;
    }

    private bool TryTicketAntags(Dictionary<ICommonSession, float> weightedSessions, ref List<AntagRule> antags)
    {
        while (RobustRandom.TryPickAndTake(weightedSessions, out var session))
        {
            if (antags.Count == 0)
                return true;

            // If this session cannot be an antag, then get the next session!
            if (!TryGetValidAntagPreferences(session, out var prefs))
                continue;

            for (var i = antags.Count - 1; i >= 0; i--)
            {
                var antag = antags[i];
                if (!PrefsContain(prefs, antag.Definition.PrefRoles))
                    continue;

                // We break it up like this to not log the server trying to make sessions without valid antag prefs into antags.
                if (!CanBeAntag(session, antag, false))
                    continue;

                // Assign the antag, and then if we finish assigning antags, remove it from the list.
                // The list doesn't need to stay organized because its order is completely arbitrary.
                if (TryTicketAntags(session, ref antag))
                    antags.RemoveSwap(i);

                break;
            }
        }

        // If we're here, then we didn't assign all the antags available!
        return false;
    }

    /// <summary>
    /// Makes this session into the given antag definition for the game rule, then decreases the count from the AntagRole by 1.
    /// If Count reaches zero we return true to say that we've finished ticketing this antag rule.
    /// </summary>
    private bool TryTicketAntags(ICommonSession session, ref AntagRule rule)
    {
        if (!AssignSessionsAntagonist(rule.GameRule, rule.Definition, session))
            return false; // Something has gone horribly wrong if this happens, check your error log!

        rule.Count--;
        if (rule.Count == 0)
            return true;

        return false;
    }

    /// <summary>
    /// Checks all preferences from a session to see if they match any of the valid roles from a list of roles available.
    /// </summary>
    /// <param name="prefs">Antag preferences, this list *should* be prefiltered for bans hence private method</param>
    /// <param name="roles">List of roles we are searching for.</param>
    /// <returns>True if any preferences match roles available.</returns>
    private bool PrefsContain(List<ProtoId<AntagPrototype>> prefs, List<ProtoId<AntagPrototype>> roles)
    {
        foreach (var pref in prefs)
        {
            if (roles.Contains(pref))
                return true;
        }

        return false;
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
    [Obsolete]
    public void ChooseAntags(Entity<AntagSelectionComponent> ent, IList<ICommonSession> pool, bool midround = false)
    {
        foreach (var def in ent.Comp.Antags)
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
    [Obsolete]
    public void ChooseAntags(Entity<AntagSelectionComponent> ent,
        IList<ICommonSession> pool,
        ProtoId<AntagSpecifierPrototype> def,
        bool midround = false)
    {
        if (!Proto.Resolve(def, out var antag))
            return;

        ChooseAntags(ent, pool, antag, midround);
    }

    [Obsolete]
    public void ChooseAntags(Entity<AntagSelectionComponent> ent,
        IList<ICommonSession> pool,
        AntagSpecifierPrototype def,
        bool midround = false)
    {
        var playerPool = GetPlayerPool(ent, pool, def);
        // TODO: Shouldn't this already be cached?
        var existingAntagCount = ent.Comp.PreSelectedSessions.TryGetValue(def, out var existingAntags) ? existingAntags.Count : 0;
        var count = GetTargetAntagCount(ent, GetActivePlayerCount(pool), def) - existingAntagCount;

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
    // TODO: Probably fine, but I need to double check the logic:tm:
    [Obsolete]
    private void AssignPreSelectedSessions(Entity<AntagSelectionComponent> ent)
    {
        // Only assign if there's been a pre-selection, and the selection hasn't already been made
        if (!ent.Comp.PreSelectionsComplete || ent.Comp.AssignmentComplete)
            return;

        foreach (var proto in ent.Comp.Antags)
        {
            if (!Proto.Resolve(proto, out var def) || !ent.Comp.PreSelectedSessions.TryGetValue(proto, out var set))
                continue;

            foreach (var session in set)
            {
                TryMakeAntag(ent, session, def);
            }
        }

        ent.Comp.AssignmentComplete = true;
    }

    /// <summary>
    /// Create an antag spawner which can be taken over by a player through the ghost role system.
    /// </summary>
    /// <param name="ent">Antag rule entity</param>
    /// <param name="def">Antag selection definition chosen from the entity</param>
    private EntityUid? CreateAntagSpawner(Entity<AntagSelectionComponent> ent, AntagSpecifierPrototype def)
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
    [Obsolete]
    private void PreSelectSessionForAntagonist(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSpecifierPrototype def)
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
    [Obsolete]
    private EntityUid? AttachSessionToAntagonist(Entity<AntagSelectionComponent> ent,
        ICommonSession session,
        AntagSpecifierPrototype def,
        MapCoordinates coords)
    {
        PreSelectSessionForAntagonist(ent, session, def);
        //ent.Comp.AssignedSessions.Add(session);
        return SpawnNewAntagonist(ent, session, def, coords);
    }

    [Obsolete]
    private EntityUid? MakeSessionAntagonist(Entity<AntagSelectionComponent> ent, ICommonSession session, ProtoId<AntagSpecifierPrototype> def)
    {
        if (!Proto.Resolve(def, out var specifier))
            return null;

        return MakeSessionAntagonist(ent, session, specifier);
    }

    /// <summary>
    /// Makes a specified player into a specified antagonist.
    /// If the player is a ghost or has no attached entity, it will attempt to find a valid spawn position and spawn a new entity.
    /// Otherwise, it will try to move their current entity to their antag's spawn position (if it exists) and then set them up as antag.
    /// </summary>
    [Obsolete]
    private EntityUid? MakeSessionAntagonist(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSpecifierPrototype def)
    {
        PreSelectSessionForAntagonist(ent, session, def);

        //ent.Comp.AssignedSessions.Add(session);

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
    [Obsolete]
    private EntityUid? SpawnNewAntagonist(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSpecifierPrototype def)
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
    [Obsolete]
    private EntityUid? SpawnNewAntagonist(Entity<AntagSelectionComponent> ent, ICommonSession session, AntagSpecifierPrototype def, MapCoordinates coordinates)
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
    [Obsolete]
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
    [Obsolete]
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
    private void InitializeAntag(Entity<AntagSelectionComponent> ent, EntityUid antag, ICommonSession? session, AntagSpecifierPrototype def)
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
            if (ent.Comp.AssignedMinds.TryGetValue(def.ID, out var minds))
            {
                minds.Add((curMind.Value, Name(antag)));
            }
            else
            {
                var hashset = new HashSet<(EntityUid, string)>();
                hashset.Add((curMind.Value, Name(antag)));
                ent.Comp.AssignedMinds.Add(def.ID, hashset);
            }

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
    [Obsolete]
    public AntagSelectionPlayerPool GetPlayerPool(Entity<AntagSelectionComponent> ent, IList<ICommonSession> sessions, AntagSpecifierPrototype def)
    {
        var preferredList = new List<ICommonSession>();
        foreach (var session in sessions)
        {
            if (!IsSessionValid(session, def) || !IsEntityValid(session.AttachedEntity, def))
                continue;

            if (ent.Comp.PreSelectedSessions.TryGetValue(def, out var preSelected) && preSelected.Contains(session))
                continue;

            // Add player to the appropriate antag pool
            if (TryGetValidAntagPreferences(session, def.PrefRoles))
            {
                preferredList.Add(session);
            }
        }

        return new AntagSelectionPlayerPool(preferredList);
    }

    private void OnObjectivesTextGetInfo(Entity<AntagSelectionComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        if (ent.Comp.AgentName is not { } name)
            return;

        args.Minds = GetAntagIdentities(ent.AsNullable()).ToList();
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
public readonly record struct AfterAntagEntitySelectedEvent(ICommonSession? Session, EntityUid EntityUid, Entity<AntagSelectionComponent> GameRule, AntagSpecifierPrototype Def);

/// <summary>
/// A given antag definition provided by a game rule.
/// This struct is created to store data for ticketing multiple antags out at once, typically for multiple gamerules, and then is destroyed when <see cref="Count"/> reaches 0.
/// </summary>
/// <param name="GameRule">The game rule which has the specified antag.</param>
/// <param name="Definition">The specified antag.</param>
/// <param name="Count">The number of specified antags left to ticket. This value does change as antags are assigned.</param>
public record struct AntagRule(Entity<AntagSelectionComponent> GameRule, AntagSpecifierPrototype Definition, int Count)
{
    /// <summary>
    /// Remaining number of times this rule can be ticketed.
    /// </summary>
    public int Count = Count;

    public static implicit operator AntagRule((Entity<AntagSelectionComponent> GameRule, AntagSpecifierPrototype Defintion) tuple)
    {
        return new AntagRule(tuple.GameRule, tuple.Defintion, 1);
    }

    public static implicit operator AntagRule((Entity<AntagSelectionComponent> GameRule, AntagSpecifierPrototype Defintion, int Count) triple)
    {
        return new AntagRule(triple.GameRule, triple.Defintion, triple.Count);
    }
}
