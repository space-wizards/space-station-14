using System.Linq;
using Content.Server.Antag.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Shuttles.Components;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Antag;
using Content.Shared.Clothing;
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
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem : GameRuleSystem<AntagSelectionComponent>
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTime = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IServerPreferencesManager _pref = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    // arbitrary random number to give late joining some mild interest.
    public const float LateJoinRandomChance = 0.5f;

    public Dictionary<NetUserId, (ICommonSession, AntagSelectionDefinition, Entity<AntagSelectionComponent>)> QueuedAntags = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        Log.Level = LogLevel.Debug;

        SubscribeLocalEvent<GhostRoleAntagSpawnerComponent, TakeGhostRoleEvent>(OnTakeGhostRole);

        SubscribeLocalEvent<AntagSelectionComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);

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

        var query = QueryAllRules(); //imp edit
        while (query.MoveNext(out var uid, out var comp, out _))
        {

            if (comp.SelectionsComplete)
                continue;

            ChooseAntags((uid, comp), pool);

            if (comp.SelectionTime != AntagSelectionTime.PrePlayerSpawn)
            {
                continue;
            }

            foreach (var session in comp.ProcessedSessions)
            {
                args.PlayerPool.Remove(session);
                GameTicker.PlayerJoinGame(session);
            }
        } // imp edit end
    }

    private void OnJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            if (comp.SelectionTime != AntagSelectionTime.PostPlayerSpawn)
                continue;

            if (!comp.SelectionsComplete) // imp edit, should never happen
                ChooseAntags((uid, comp), args.Players);

            foreach (var (_, antagData) in QueuedAntags)
            {
                if (antagData.Item3.Comp == comp)
                    MakeAntag(antagData.Item3, antagData.Item1, antagData.Item2);
            }

        } //end imp edit
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!args.LateJoin)
            return;

        // TODO: this really doesn't handle multiple latejoin definitions well
        // eventually this should probably store the players per definition with some kind of unique identifier.
        // something to figure out later.

        var query = QueryActiveRules();
        var rules = new List<(EntityUid, AntagSelectionComponent)>();
        while (query.MoveNext(out var uid, out _, out var antag, out _))
        {
            rules.Add((uid, antag));
        }
        RobustRandom.Shuffle(rules);

        foreach (var (uid, antag) in rules)
        {
            if (!antag.Definitions.Any(p => p.ForceAllPossible))
                if (!RobustRandom.Prob(LateJoinRandomChance))
                    continue;

            if (!antag.Definitions.Any(p => p.LateJoinAdditional))
                continue;

            DebugTools.AssertEqual(antag.SelectionTime, AntagSelectionTime.PostPlayerSpawn);

            // do not count players in the lobby for the antag ratio
            var players = _player.NetworkedSessions.Count(x => x.AttachedEntity != null);

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

        if (component.SelectionsComplete) // Imp edit start
        {
            if (QueuedAntags.Count != 0)
            {
                foreach (var (_, antagData) in QueuedAntags)
                {
                    if (antagData.Item3.Comp == component)
                        MakeAntag(antagData.Item3, antagData.Item1, antagData.Item2);

                }
                // Checking if antag counts meet expectations and choosing additional antags if not
                var existingAntags = GetAntagMinds((uid, component)).Count;
                var targetCount = GetTargetAntagCount((uid, component));
                if (existingAntags < targetCount)
                {
                    var playerPool = _player.Sessions
                                .Where(x => GameTicker.PlayerGameStatuses.TryGetValue(x.UserId, out var status) && status == PlayerGameStatus.JoinedGame)
                                .ToList();
                    if (TryGetNextAvailableDefinition((uid, component), out var def)) // Given how we're getting here this should never be false but I'm wrapping it like this anyway Because
                    {
                        ChooseAntags((uid, component), playerPool, (AntagSelectionDefinition)def, midround: true, targetCount - existingAntags);
                    }
                }
            }
            else
                return;
        } // Imp edit end

        var players = _player.Sessions
            .Where(x => GameTicker.PlayerGameStatuses.TryGetValue(x.UserId, out var status) && status == PlayerGameStatus.JoinedGame)
            .ToList();

        ChooseAntags((uid, component), players, midround: true);
    }

    /// <summary>
    /// Chooses antagonists from the given selection of players
    /// </summary>
    /// <param name="ent">The antagonist rule entity</param>
    /// <param name="pool">The players to choose from</param>
    /// <param name="midround">Disable picking players for pre-spawn antags in the middle of a round</param>
    public void ChooseAntags(Entity<AntagSelectionComponent> ent, IList<ICommonSession> pool, bool midround = false)
    {
        if (ent.Comp.SelectionsComplete)
            return;

        foreach (var def in ent.Comp.Definitions)
        {
            ChooseAntags(ent, pool, def, midround: midround);
        }

        ent.Comp.SelectionsComplete = true;
    }

    /// <summary>
    /// Chooses antagonists from the given selection of players for the given antag definition.
    /// </summary>
    /// <param name="ent">The antagonist rule entity</param>
    /// <param name="pool">The players to choose from</param>
    /// <param name="def">The antagonist selection parameters and criteria</param>
    /// <param name="midround">Disable picking players for pre-spawn antags in the middle of a round</param>
    /// <param name="number">Override to choose a number of additional antags if there are not enough at the start of the gamerule. </param>
    public void ChooseAntags(Entity<AntagSelectionComponent> ent,
        IList<ICommonSession> pool,
        AntagSelectionDefinition def,
        bool midround = false,
        int number = 0) //imp edit
    {
        var playerPool = GetPlayerPool(ent, pool, def);
        var count = number;
        if (count <= 0)
            count = GetTargetAntagCount(ent, GetTotalPlayerCount(pool), def);

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

        // imp begin. this is our playtime biasing solution.
        // create a dictionary of player sessions paired with the total antag playtime that player has across mindroles in this rule.
        Dictionary<ICommonSession, TimeSpan> sessionAndRoleTimes = [];
        foreach (var session in playerPool.GetPoolSessions())
        {
            // if we've picked a valid session from the pool and there are mindroles to assign in the def,
            if (session != null && def.PrefRoles != null)
            {
                var ruleTimeTotal = TimeSpan.Zero;
                // grab this session's playtimes for each role,
                foreach (var role in def.PrefRoles)
                {
                    TimeSpan? time = null;
                    if (_prototype.TryIndex(role, out var antagRole))
                    {
                        _playTime.TryGetTrackerTime(session, antagRole.PlayTimeTracker, out time);
                    }
                    ruleTimeTotal += time != null ? time.Value : TimeSpan.Zero;
                }
                // add them to our dict,
                sessionAndRoleTimes.Add(session, ruleTimeTotal);
            }
        }
        // then sort our dict by role time.
        var playersByRoleTimeAsc = from entry in sessionAndRoleTimes orderby entry.Value ascending select entry;

        // now we do playtime biasing.
        var probToGuarantee = 0.3f; // the highest chance of getting a guaranteed spot. given to the person queued with the lowest mindrole playtime.
        var probReduction = probToGuarantee / ((float)playersByRoleTimeAsc.Count() / 2f); // linearly reduces the probability so that it hits zero after going through half of the players. NOTE: might tweak this to take the desired count instead of total players queued for this antag.
        List<ICommonSession> guaranteed = [];
        foreach (var keyValuePair in playersByRoleTimeAsc) // for each entry, decide whether or not it should override random antag selection based on its weight, and add it to a list if it should.
        {
            if (HasPrimaryAntagPreference(keyValuePair.Key, def) && _random.Prob(probToGuarantee))
            {
                guaranteed.Add(keyValuePair.Key);
            }
            probToGuarantee -= probReduction; // reduce the probability of the next entry getting a guaranteed slot by (maximum prob / (total queried players / 2))
            if (probToGuarantee <= 0 || guaranteed.Count == count) // stop the loop if the next probability is less than or equal to 0, or if the guaranteed list has hit the target antag count.
                break;
        }

        for (var i = 0; i < count; i++)
        {
            var session = (ICommonSession?)null;
            // if the playtime bias system picked any guaranteed antags,
            if (guaranteed.Count > 0)
            {
                session = guaranteed[0]; // set this session as the picked session and proceed through the rest of the process
                guaranteed.RemoveAt(0);
            }
            if (picking && session == null)
            {
                if (!playerPool.TryPickAndTake(RobustRandom, out session) && noSpawner)
                {
                    Log.Warning($"Couldn't pick a player for {ToPrettyString(ent):rule}, no longer choosing antags for this definition");
                    break;
                }

                if (session != null && QueuedAntags.ContainsKey(session.UserId))
                {
                    Log.Warning($"Somehow picked {session} for an antag when another rule already selected them previously");
                    continue;
                }
            }
            if (!midround && ent.Comp.SelectionTime != AntagSelectionTime.PrePlayerSpawn && session != null) //Midround rule additions, ghost roles, and prespawn activations should never be queued
            {
                ent.Comp.SelectedSessions.Add(session);
                QueuedAntags[session.UserId] = (session, def, ent);
            }
            else
                MakeAntag(ent, session, def);
        } //end imp edit
    }

    /// <summary>
    /// Tries to makes a given player into the specified antagonist.
    /// </summary>
    public bool TryMakeAntag(Entity<AntagSelectionComponent> ent, ICommonSession? session, AntagSelectionDefinition def, bool ignoreSpawner = false, bool checkPref = true)
    {
        if (checkPref && !HasPrimaryAntagPreference(session, def))
            return false;

        if (!IsSessionValid(ent, session, def) || !IsEntityValid(session?.AttachedEntity, def))
            return false;

        MakeAntag(ent, session, def, ignoreSpawner);
        return true;
    }

    /// <summary>
    /// Makes a given player into the specified antagonist.
    /// </summary>
    public void MakeAntag(Entity<AntagSelectionComponent> ent, ICommonSession? session, AntagSelectionDefinition def, bool ignoreSpawner = false)
    {
        EntityUid? antagEnt = null;
        var isSpawner = false;

        if (session != null) //imp edit
        {
            ent.Comp.SelectedSessions.Remove(session);
            QueuedAntags.Remove(session.UserId);
            ent.Comp.ProcessedSessions.Add(session);

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
            if (session != null)
                ent.Comp.ProcessedSessions.Remove(session);
            return;
        }

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
                if (session != null)
                    ent.Comp.SelectedSessions.Remove(session);
                return;
            }

            spawnerComp.Rule = ent;
            spawnerComp.Definition = def;
            return;
        }

        var prereqEv = new AntagPrereqSetupEvent(session, ent, def);
        RaiseLocalEvent(ent, ref prereqEv, true);

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
            ent.Comp.SelectedMinds.Add((curMind.Value, Name(player)));
            SendBriefing(session, def.Briefing);

            Log.Debug($"Selected {ToPrettyString(curMind)} as antagonist: {ToPrettyString(ent)}");
        }

        var afterEv = new AfterAntagEntitySelectedEvent(session, player, ent, def);
        RaiseLocalEvent(ent, ref afterEv, true);
    } //end imp edit

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
            if (!HasValidAntagJobs(session)) //imp edit
                continue;
            if (HasPrimaryAntagPreference(session, def))
            {
                preferredList.Add(session);
            }
            else if (HasFallbackAntagPreference(session, def))
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

        if (ent.Comp.ProcessedSessions.Contains(session))
            return false;

        mind ??= session.GetMind();

        // If the player has not spawned in as any entity (e.g., in the lobby), they can be given an antag role/entity.
        if (mind == null)
            return true;

        //todo: we need some way to check that we're not getting the same role twice. (double picking thieves or zombies through midrounds)

        switch (def.MultiAntagSetting)
        {
            case AntagAcceptability.None:
            {
                if (_role.MindIsAntagonist(mind))
                    return false;
                break;
            }
            case AntagAcceptability.NotExclusive:
            {
                if (_role.MindIsExclusiveAntagonist(mind))
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

        args.Minds = ent.Comp.SelectedMinds;
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
/// Event raised on a game rule entity to send additional information before begining setup.
/// Used for applying additional more complex setup logic.
/// </summary>
[ByRefEvent]
public readonly record struct AntagPrereqSetupEvent(ICommonSession? Session, Entity<AntagSelectionComponent> GameRule, AntagSelectionDefinition Def);

/// <summary>
/// Event raised on a game rule entity after the setup logic for an antag is complete.
/// Used for applying additional more complex setup logic.
/// </summary>
[ByRefEvent]
public readonly record struct AfterAntagEntitySelectedEvent(ICommonSession? Session, EntityUid EntityUid, Entity<AntagSelectionComponent> GameRule, AntagSelectionDefinition Def);
