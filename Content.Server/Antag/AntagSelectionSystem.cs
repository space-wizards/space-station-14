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
using Content.Server.Shuttles.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Antag;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
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

        MakeAntag((rule, select), args.Player, def, ignoreSpawner: true);
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

            if (TryMakeAntag((uid, antag), args.Player, def.Value))
                break;
        }
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
        var existingAntagCount = ent.Comp.PreSelectedSessions.TryGetValue(def, out var existingAntags) ?  existingAntags.Count : 0;
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
                MakeAntag(ent, null, def); // This is for spawner antags
            else
            {
                if (!ent.Comp.PreSelectedSessions.TryGetValue(def, out var set))
                    ent.Comp.PreSelectedSessions.Add(def, set = new HashSet<ICommonSession>());
                set.Add(session); // Selection done!
                Log.Debug($"Pre-selected {session.Name} as antagonist: {ToPrettyString(ent)}");
                _adminLogger.Add(LogType.AntagSelection, $"Pre-selected {session.Name} as antagonist: {ToPrettyString(ent)}");
            }
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
    public bool TryMakeAntag(Entity<AntagSelectionComponent> ent, ICommonSession? session, AntagSelectionDefinition def, bool ignoreSpawner = false, bool checkPref = true, bool onlyPreSelect = false)
    {
        _adminLogger.Add(LogType.AntagSelection, $"Start trying to make {session} become the antagonist: {ToPrettyString(ent)}");

        if (checkPref && !ValidAntagPreference(session, def.PrefRoles))
            return false;

        if (!IsSessionValid(ent, session, def) || !IsEntityValid(session?.AttachedEntity, def))
            return false;

        if (onlyPreSelect && session != null)
        {
            if (!ent.Comp.PreSelectedSessions.TryGetValue(def, out var set))
                ent.Comp.PreSelectedSessions.Add(def, set = new HashSet<ICommonSession>());
            set.Add(session);
            Log.Debug($"Pre-selected {session!.Name} as antagonist: {ToPrettyString(ent)}");
            _adminLogger.Add(LogType.AntagSelection, $"Pre-selected {session.Name} as antagonist: {ToPrettyString(ent)}");
        }
        else
        {
            MakeAntag(ent, session, def, ignoreSpawner);
        }

        return true;
    }

    /// <summary>
    /// Makes a given player into the specified antagonist.
    /// </summary>
    public void MakeAntag(Entity<AntagSelectionComponent> ent, ICommonSession? session, AntagSelectionDefinition def, bool ignoreSpawner = false)
    {
        EntityUid? antagEnt = null;
        var isSpawner = false;

        if (session != null)
        {
            if (!ent.Comp.PreSelectedSessions.TryGetValue(def, out var set))
                ent.Comp.PreSelectedSessions.Add(def, set = new HashSet<ICommonSession>());
            set.Add(session);
            ent.Comp.AssignedSessions.Add(session);

            // we shouldn't be blocking the entity if they're just a ghost or smth.
            if (!HasComp<GhostComponent>(session.AttachedEntity))
                antagEnt = session.AttachedEntity;
        }
        else if (!ignoreSpawner && def.SpawnerPrototype != null) // don't add spawners if we have a player, dummy.
        {
            antagEnt = Spawn(def.SpawnerPrototype);
            isSpawner = true;
        }

        if (!antagEnt.HasValue)
        {
            var getEntEv = new AntagSelectEntityEvent(session, ent);
            RaiseLocalEvent(ent, ref getEntEv, true);
            antagEnt = getEntEv.Entity;
        }

        if (antagEnt is not { } player)
        {
            Log.Error($"Attempted to make {session} antagonist in gamerule {ToPrettyString(ent)} but there was no valid entity for player.");
            _adminLogger.Add(LogType.AntagSelection,$"Attempted to make {session} antagonist in gamerule {ToPrettyString(ent)} but there was no valid entity for player.");
            if (session != null && ent.Comp.RemoveUponFailedSpawn)
            {
                ent.Comp.AssignedSessions.Remove(session);
                ent.Comp.PreSelectedSessions[def].Remove(session);
            }

            return;
        }

        // TODO: This is really messy because this part runs twice for midround events.
        // Once when the ghostrole spawner is created and once when a player takes it.
        // Therefore any component subscribing to this has to make sure both subscriptions return the same value
        // or the ghost role raffle location preview will be wrong.

        var getPosEv = new AntagSelectLocationEvent(session, ent);
        RaiseLocalEvent(ent, ref getPosEv, true);
        if (getPosEv.Handled)
        {
            var playerXform = Transform(player);
            var pos = RobustRandom.Pick(getPosEv.Coordinates);
            _transform.SetMapCoordinates((player, playerXform), pos);
        }

        // If we want to just do a ghost role spawner, set up data here and then return early.
        // This could probably be an event in the future if we want to be more refined about it.
        if (isSpawner)
        {
            if (!TryComp<GhostRoleAntagSpawnerComponent>(player, out var spawnerComp))
            {
                Log.Error($"Antag spawner {player} does not have a GhostRoleAntagSpawnerComponent.");
                _adminLogger.Add(LogType.AntagSelection,$"Antag spawner {player} in gamerule {ToPrettyString(ent)} failed due to not having GhostRoleAntagSpawnerComponent.");
                if (session != null)
                {
                    ent.Comp.AssignedSessions.Remove(session);
                    ent.Comp.PreSelectedSessions[def].Remove(session);
                }

                return;
            }

            spawnerComp.Rule = ent;
            spawnerComp.Definition = def;
            return;
        }

        // The following is where we apply components, equipment, and other changes to our antagonist entity.
        EntityManager.AddComponents(player, def.Components);

        // Equip the entity's RoleLoadout and LoadoutGroup
        List<ProtoId<StartingGearPrototype>> gear = new();
        if (def.StartingGear is not null)
            gear.Add(def.StartingGear.Value);

        _loadout.Equip(player, gear, def.RoleLoadout);

        if (session != null)
        {
            var curMind = session.GetMind();

            if (curMind == null ||
                !TryComp<MindComponent>(curMind.Value, out var mindComp) ||
                mindComp.OwnedEntity != antagEnt)
            {
                curMind = _mind.CreateMind(session.UserId, Name(antagEnt.Value));
                _mind.SetUserId(curMind.Value, session.UserId);
            }

            _mind.TransferTo(curMind.Value, antagEnt, ghostCheckOverride: true);
            _role.MindAddRoles(curMind.Value, def.MindRoles, null, true);
            ent.Comp.AssignedMinds.Add((curMind.Value, Name(player)));
            SendBriefing(session, def.Briefing);

            Log.Debug($"Assigned {ToPrettyString(curMind)} as antagonist: {ToPrettyString(ent)}");
            _adminLogger.Add(LogType.AntagSelection, $"Assigned {ToPrettyString(curMind)} as antagonist: {ToPrettyString(ent)}");
        }

        var afterEv = new AfterAntagEntitySelectedEvent(session, player, ent, def);
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

        if (HasComp<PendingClockInComponent>(entity))
            return false;

        if (!def.AllowNonHumans && !HasComp<HumanoidAppearanceComponent>(entity))
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
[ByRefEvent]
public record struct AntagSelectEntityEvent(ICommonSession? Session, Entity<AntagSelectionComponent> GameRule)
{
    public readonly ICommonSession? Session = Session;

    public bool Handled => Entity != null;

    public EntityUid? Entity;
}

/// <summary>
/// Event raised on a game rule entity to determine the location for the antagonist.
/// </summary>
[ByRefEvent]
public record struct AntagSelectLocationEvent(ICommonSession? Session, Entity<AntagSelectionComponent> GameRule)
{
    public readonly ICommonSession? Session = Session;

    public bool Handled => Coordinates.Any();

    public List<MapCoordinates> Coordinates = new();
}

/// <summary>
/// Event raised on a game rule entity after the setup logic for an antag is complete.
/// Used for applying additional more complex setup logic.
/// </summary>
[ByRefEvent]
public readonly record struct AfterAntagEntitySelectedEvent(ICommonSession? Session, EntityUid EntityUid, Entity<AntagSelectionComponent> GameRule, AntagSelectionDefinition Def);
