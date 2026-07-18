using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using static Content.Server.Antag.Components.AntagSelectionTime;

namespace Content.Server.Antag;

/// <summary>
/// Turns players into antags.
/// When the round starts, all active game rules select players for antagonist.
/// When a game rule is started, all selected players are given their antagonist status (including entities and components)
/// If selection was not done before the game rule has been started, it will happen during that step.
/// Antag entities spawned by this system are always prioritized over the player's current entity.
/// </summary>
/// <remarks>
/// I leave this remark here as a reminder of two things:
/// Never initialize entities while they're still in nullspace, I had to refactor this system to fix that.
/// Do not touch the spawning logic unless you understand how spawning works in engine to ensure the above.
/// Never do a patchwork refactor for a bad system, I had to refactor this system twice because of that mistake.
/// I hope this system is now readable and significantly less buggy thanks to my efforts.
/// I could do more, but I've been soaped enough. Now it's your turn to fix it.
/// </remarks>
public sealed partial class AntagSelectionSystem : GameRuleSystem<AntagSelectionComponent>
{
    [Dependency] private IBanManager _ban = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IServerPreferencesManager _pref = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ArrivalsSystem _arrivals = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private FollowerSystem _follower = default!;
    [Dependency] private GhostRoleSystem _ghostRole = default!;
    [Dependency] private JobSystem _jobs = default!;
    [Dependency] private LoadoutSystem _loadout = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private TransformSystem _transform = default!;

    // arbitrary random number to give late joining some mild interest.
    public const float LateJoinRandomChance = 0.5f;

    /// <summary>
    /// List of game rules and antags that are assigned during <see cref="RulePlayerSpawningEvent"/>
    /// Should only ever include game rules with <see cref="AntagSelectionComponent.SelectionTime"/> of <see cref="AntagSelectionTime.PrePlayerSpawn"/>
    /// </summary>
    private List<AntagRule>? _preSpawnRules;

    /// <summary>
    /// List of game rules and antags that are assigned during <see cref="RulePlayerJobsAssignedEvent"/>
    /// Includes both game rules with <see cref="AntagSelectionComponent.SelectionTime"/> of <see cref="AntagSelectionTime.JobsAssigned"/>
    /// and active game rules with <see cref="AntagSelectionComponent.SelectionTime"/> of <see cref="AntagSelectionTime.RuleStarted"/>
    /// </summary>
    private List<AntagRule>? _postSpawnRules;

    /// <summary>
    /// A list of players which were selected by a game rule for a specific antag during <see cref="RulePlayerSpawningEvent"/>
    /// but were not spawned during that step, and now must be spawned during <see cref="RulePlayerJobsAssignedEvent"/>.
    /// This is also used to check for errors during <see cref="GameRuleStartedEvent"/> to see if any players were assigned
    /// </summary>
    private List<(Entity<AntagSelectionComponent> gameRule, AntagSpecifierPrototype antag, ICommonSession player)> _delayedAntags = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        Log.Level = LogLevel.Debug;

        SubscribeLocalEvent<GhostRoleAntagSpawnerComponent, TakeGhostRoleEvent>(OnTakeGhostRole);

        SubscribeLocalEvent<AntagSelectionComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);

        // In order of how these occur.
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayerSpawning);
        SubscribeLocalEvent<NoJobsAvailableSpawningEvent>(OnJobNotAssigned);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobsAssigned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
    }

    protected override void Started(EntityUid uid, AntagSelectionComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // If we're not in round, don't spawn or assign antags. Those will be handled by RulePlayerSpawning, and RulePlayerJobs
        if (GameTicker.RunLevel != GameRunLevel.InRound)
            return;

        if (component.AssignmentHandled)
            return;

        // Antags haven't been selected so we need to select them! Only if we select when the game rule starts though!
        if (component.PreSelectionsComplete)
        {
            AssignPreSelectedSessions((uid, component));
            return;
        }

        // If pre-selections haven't completed, then we need to select and assign antags.
        var players = GetActivePlayers().ToArray();

        if (component.SelectionTime == RuleStarted) // Only pre-select antags if we pre-select on rule start
            AssignAntags((uid, component), players);
        else // Otherwise, we only spawn the ghost roles!
            SpawnGhostRoles((uid, component), players.Length);
    }

    private void OnTakeGhostRole(Entity<GhostRoleAntagSpawnerComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.TookRole)
            return;

        if (ent.Comp.Rule is not { } rule || ent.Comp.Definition is not { } proto)
            return;

        if (!ProtoMan.Resolve(proto, out var def))
            return;

        if (!Exists(rule) || !RuleQuery.TryComp(rule, out var select))
            return;

        // Ensure the player is allowed to play this antagonist!
        if (IsAntagBanned(args.Player, def) || !_playTime.IsAllowed(args.Player, def.PrefRoles))
            return;

        if (!TrySpawnAntagonist((rule, select), def, args.Player, _transform.GetMapCoordinates(ent), out var uid))
        {
            Log.Error($"Tried to make {args.Player.UserId} into an antagonist but was unable to spawn an entity for them. Game rule {ToPrettyString(ent)}");
            return;
        }

        // We do this after TrySpawnAntagonist so we don't have to worry about a failed spawn adding permanent pre selections to a game rule.
        PreSelectSession((rule, select), def, args.Player);
        InitializeAntag((rule, select), def, uid.Value, args.Player);
        args.TookRole = true;

        // Move ghosts that were watching the raffle on the spawner over to the freshly spawned antag.
        _follower.TransferFollowers(ent.Owner, uid.Value);

        _ghostRole.UnregisterGhostRole((ent, Comp<GhostRoleComponent>(ent)));
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!args.LateJoin)
            return;

        TryMakeLateJoinAntag(args.Player);
    }

    // This is called when the round starts, before jobs are selected
    private void OnPlayerSpawning(RulePlayerSpawningEvent args)
    {
        var pool = args.PlayerPool;

        // Get all GameRules and store all antags from them in two lists, one we query now and another we query later!
        _preSpawnRules = [];
        _postSpawnRules = [];
        var rulesQuery = QueryAllRules();
        while (rulesQuery.MoveNext(out var uid, out var antag, out var rule))
        {
            // Add it to the list of pre selections then mark it as complete.
            // This is the best query to do it in, and we're not returning early so might as well do it here.
            AddGameRuleDefinitions((uid, antag), pool.Count, ref _preSpawnRules, ref _postSpawnRules, GameTicker.IsGameRuleActive(uid, rule));
            antag.PreSelectionsComplete = true;
        }

        // Pick a random player session and then try to assign the currently available antags from it!
        // This means each player has the same chance at rolling antag, with minimal alterations to the odds by number of antags selected.
        var weightedPool = GetWeightedPlayerPool(pool);
        while (RobustRandom.TryPickAndTake(weightedPool, out var session))
        {
            // Antag distributed so we remove the session.
            if (!PreAssignAntag(session, ref _preSpawnRules))
                continue;

            args.PlayerPool.Remove(session);
            GameTicker.PlayerJoinGame(session);
        }

        // Make ghost role spawners for any remaining rules!
        SpawnGhostRoles(_preSpawnRules);
        _preSpawnRules = null; // Clear the list, we don't want it anymore
    }

    private void OnJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        if (_postSpawnRules == null)
        {
            Log.Error($"Error! _postSpawnRules was null when {nameof(RulePlayerJobsAssignedEvent)} was run, this should have been initialized and populated before jobs were assigned.");
            return;
        }

        // Pick a random player session and then try to assign the currently available antags from it!
        // This means each player has the same chance at rolling antag, with minimal alterations to the odds by number of antags selected.
        var weightedPool = GetWeightedPlayerPool(args.Players);
        while (RobustRandom.TryPickAndTake(weightedPool, out var session))
        {
            AssignAntag(session, ref _postSpawnRules);
        }

        // Make ghost role spawners for any remaining rules!
        SpawnGhostRoles(_postSpawnRules);
        _postSpawnRules = null; // Clear the list since it's been used up!

        foreach (var antag in _delayedAntags)
        {
            if (!TryInitializeAntag(antag.gameRule, antag.antag, antag.player))
                Log.Error($"Gamerule {ToPrettyString(antag.gameRule)} failed to spawn {antag.player.Name} as antag {antag.antag.ID} after spawning.");
        }

        _delayedAntags.Clear();
    }

    private void OnJobNotAssigned(NoJobsAvailableSpawningEvent args)
    {
        // If someone fails to spawn in due to there being no jobs, they should be removed from any preselected antags.
        // We only care about delayed rules, since if they're active the player should have already been removed via MakeAntag.
        var query = QueryDelayedRules();
        while (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            if (comp.SelectionTime == RuleStarted)
                continue;

            Debug.Assert(comp.SelectionTime != Never, $"Player: {args.Player.Name}, was pre selected for an game rule {ToPrettyString(uid)} which does not do pre-selections");

            if (!comp.RemoveUponFailedSpawn)
                continue;

            foreach (var antag in comp.Antags)
            {
                if (!comp.PreSelectedSessions.TryGetValue(antag, out var session))
                    break;
                session.Remove(args.Player);
            }
        }
    }

    private void AddGameRuleDefinitions(Entity<AntagSelectionComponent> gameRule,
        int playerCount,
        ref List<AntagRule> preSpawnRoles,
        ref List<AntagRule> postSpawnRoles,
        bool active)
    {
        switch (gameRule.Comp.SelectionTime)
        {
            case PrePlayerSpawn:
                AddGameRuleDefinitions(gameRule, playerCount, ref preSpawnRoles, active);
                break;
            case JobsAssigned:
                AddGameRuleDefinitions(gameRule, playerCount, ref postSpawnRoles, active);
                break;
            case RuleStarted:
                if (active) // Only if the game rule is active to we preselect, since the event for activation already ran and was skipped.
                    AddGameRuleDefinitions(gameRule, playerCount, ref postSpawnRoles, active);
                break;
            case Never:
                SpawnGhostRoles(gameRule, playerCount, true);
                break;
        }
    }

    private void AddGameRuleDefinitions(Entity<AntagSelectionComponent> gameRule,
        int playerCount,
        ref List<AntagRule> roles,
        bool active)
    {
        var runningCount = 0;

        foreach (var antag in gameRule.Comp.Antags)
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
                MakeAntag(ent, null, def); // This is for spawner antags
            else
            {
                if (!ent.Comp.PreSelectedSessions.TryGetValue(def, out var set))
                    ent.Comp.PreSelectedSessions.Add(def, set = new HashSet<ICommonSession>());
                set.Add(session); // Selection done!
                Log.Debug($"Pre-selected {session.Name} as antagonist: {ToPrettyString(ent)}");
                _adminLogger.Add(LogType.AntagSelection, LogImpact.Medium, $"Pre-selected {session.Name} as antagonist: {ent.Owner:entity}");
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

            // We do it this way in case our resolve fails.
            roles.Add((gameRule, proto, active, GetTargetAntagCount(antag, playerCount, ref runningCount)));
        }
    }

    private List<AntagCount> GetAntags(Entity<AntagSelectionComponent> gameRule,
        int playerCount)
    {
        var runningCount = 0;
        var antags = new List<AntagCount>(gameRule.Comp.Antags.Length);

        // We assume that antag definitions are prioritized by order, and take up slots that other roles may take.
        // I.E for Nukies, it selects 1 commander which takes up 10 players, then one corpsman which takes up another 10, then we select X nukies based on the remaining player count.
        // This is how the system worked when I got here, and I decided not to change it to avoid fucking with team antag balance
        foreach (var antag in gameRule.Comp.Antags)
        {
            if (!ProtoMan.Resolve(antag.Proto, out var definition))
                continue;

            // We do it this way in case our resolve fails.
            antags.Add((definition, GetTargetAntagCount(antag, playerCount, ref runningCount)));
        }

        return antags;
    }

    private Dictionary<ICommonSession, float> GetWeightedPlayerPool(IEnumerable<ICommonSession> players)
    {
        var dict = new Dictionary<ICommonSession, float>();
        foreach (var player in players)
        {
            dict.Add(player, GetWeight(player));
        }

        return dict;
    }

    private float GetWeight(ICommonSession player)
    {
        // TODO: Actually add weights! This is placeholder for a future PR.
        return 1f;
    }

    private void AssignAntags(Entity<AntagSelectionComponent> gameRule)
    {
        AssignAntags(gameRule, GetActivePlayers().ToArray());
    }

    private void AssignAntags(Entity<AntagSelectionComponent> gameRule, IList<ICommonSession> players)
    {
        var antags = GetAntags(gameRule, players.Count);
        AssignAntags(gameRule, players, antags);
        gameRule.Comp.PreSelectionsComplete = true;
    }

    private void AssignAntags(Entity<AntagSelectionComponent> gameRule, IList<ICommonSession> players, List<AntagCount> antags)
    {
        AssignAntags(gameRule, GetWeightedPlayerPool(players), antags);
    }

    private void AssignAntags(Entity<AntagSelectionComponent> gameRule, Dictionary<ICommonSession, float> weightedPool, List<AntagCount> antags)
    {
        while (RobustRandom.TryPickAndTake(weightedPool, out var session))
        {
            AssignAntag(gameRule, session, ref antags);

            // Assignment complete, return early.
            if (antags.Count == 0)
                return;
        }

        // We didn't assign all antags, so we try and make ghost roles for the remaining antags!
        SpawnGhostRoles(gameRule, antags);
    }

    /// <summary>
    /// Selects and assigns antags from a list, this is called before the game has started.
    /// Is private because it has it should only ever be run in very specific scenarios.
    /// </summary>
    private bool PreAssignAntag(ICommonSession player, ref List<AntagRule> antags)
    {
        // If this session cannot be an antag, then get the next session!
        if (!TryGetValidAntagPreferences(player, out var prefs))
            return false;

        for (var i = antags.Count - 1; i >= 0; i--)
        {
            var antag = antags[i];

            // Skip definitions that don't want a player assigned to them.
            if (!antag.Definition.PickPlayer)
            {
                Debug.Assert(antag.Definition.SpawnerPrototype != null,
                    $"Antag prototype {antag.Definition.ID} was set to not pre-select, but it also had no ghost spawner to spawn.");
                continue;
            }

            if (!PrefsContain(prefs, antag.Definition.PrefRoles))
                continue;

            // We break it up like this to not log the server trying to make sessions without valid antag prefs into antags.
            if (!CanBeAntag(player, antag.GameRule, antag.Definition, false))
                continue;

            // Pre-select the session then deprecate the selection count.
            PreSelectSession(antag.GameRule, antag.Definition, player);

            // Reduce the slots left by one
            // If we finish assigning all slots
            antag.Count--;
            if (antag.Count == 0)
                antags.RemoveSwap(i);
            else
                antags[i] = antag;

            if (!antag.Active)
                return false;

            // Try to assign them an entity if the game rule allows it.
            // We don't deselect fails since we may have to wait until the player has spawned first!
            if (TryGetAntagEntity(antag.GameRule, antag.Definition, player, out var antagEnt))
            {
                InitializeAntag(antag.GameRule, antag.Definition, antagEnt.Value, player);
                return true;
            }

            // If we didn't assign an antag, try again after the player has spawned.
            _delayedAntags.Add((antag.GameRule, antag.Definition, player));
            return false;
        }

        // If we're here, then we didn't assign a single antag!
        return false;
    }

    /// <summary>
    /// Selects and assigns antags from a list, this is called before the game has started.
    /// Is private because it has it should only ever be run in very specific scenarios.
    /// </summary>
    private bool AssignAntag(ICommonSession player, ref List<AntagRule> antags)
    {
        // If this session cannot be an antag, then get the next session!
        if (!TryGetValidAntagPreferences(player, out var prefs))
            return false;

        for (var i = antags.Count - 1; i >= 0; i--)
        {
            var antag = antags[i];

            // Skip definitions that don't want a player assigned to them.
            if (!antag.Definition.PickPlayer)
            {
                Debug.Assert(antag.Definition.SpawnerPrototype != null,
                    $"Antag prototype {antag.Definition.ID} was set to not pre-select, but it also had no ghost spawner to spawn.");
                continue;
            }

            if (!PrefsContain(prefs, antag.Definition.PrefRoles))
                continue;

            // We break it up like this to not log the server trying to make sessions without valid antag prefs into antags.
            if (!CanBeAntag(player, antag, false))
                continue;

            // Try to get a valid antag entity.
            if (!TryGetAntagEntity(antag.GameRule, antag.Definition, player, out var antagEnt))
                continue; // Something has gone horribly wrong if this happens, check your error log!

            // Pre-select the sesssion, then initialize the antag!
            PreSelectSession(antag.GameRule, antag.Definition, player);
            InitializeAntag(antag.GameRule, antag.Definition, antagEnt.Value, player);

            // Reduce the slots left by one
            // If we finish assigning all slots
            antag.Count--;
            if (antag.Count == 0)
                antags.RemoveSwap(i);
            else
                antags[i] = antag;

            return true;
        }

        // If we're here, then we didn't assign a single antag!
        return false;
    }

    /// <summary>
    /// Selects and assigns antags from a list.
    /// Is private because it has it should only ever be run in very specific scenarios.
    /// </summary>
    private bool AssignAntag(Entity<AntagSelectionComponent> gameRule, ICommonSession player, ref List<AntagCount> antags)
    {
        // If this session cannot be an antag, then get the next session!
        if (!TryGetValidAntagPreferences(player, out var prefs))
            return false;

        for (var i = antags.Count - 1; i >= 0; i--)
        {
            var antag = antags[i];

            // Skip definitions that don't want a player assigned to them.
            if (!antag.Definition.PickPlayer)
            {
                Debug.Assert(antag.Definition.SpawnerPrototype != null,
                    $"Antag prototype {antag.Definition.ID} was set to not pre-select, but it also had no ghost spawner to spawn.");
                continue;
            }

            if (!PrefsContain(prefs, antag.Definition.PrefRoles))
                continue;

            // We break it up like this to not log the server trying to make sessions without valid antag prefs into antags.
            if (!CanBeAntag(player, gameRule, antag.Definition, false))
                continue;

            // Try to get a valid antag entity.
            if (!TryGetAntagEntity(gameRule, antag.Definition, player, out var antagEnt))
                continue; // Something has likely gone horribly wrong if this happens, check your error log!

            // Pre-select the session, then initialize the antag!
            PreSelectSession(gameRule, antag.Definition, player);
            InitializeAntag(gameRule, antag.Definition, antagEnt.Value, player);

            // Reduce the slots left by one
            // If we finish assigning all slots
            antag.Count--;
            if (antag.Count == 0)
                antags.RemoveSwap(i);
            else
                antags[i] = antag;

            return true;
        }

        // If we're here, then we didn't assign a single antag!
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
        foreach (var role in roles)
        {
            if (prefs.Contains(role))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Marks a player as being chosen by a game rule for antag.
    /// This happens before the antag initializes.
    /// A player will only be removed from pre-selection if they fail to initialize as antag later. Which will be logged.
    /// </summary>
    /// <param name="gameRule">Game rule which has chosen this player for antag.</param>
    /// <param name="protoId">Antag prototype this player will become.</param>
    /// <param name="player">Player.</param>
    private void PreSelectSession(Entity<AntagSelectionComponent> gameRule, ProtoId<AntagSpecifierPrototype> protoId, ICommonSession player)
    {
        if (!gameRule.Comp.PreSelectedSessions.TryGetValue(protoId, out var set))
            gameRule.Comp.PreSelectedSessions.Add(protoId, set = new HashSet<ICommonSession>());

        // Element already exists, don't need to log it twice, this typically happens when a pre-selected antag is initialized!
        if (!set.Add(player))
            return;

        Log.Debug($"Pre-selected {player.Name} as antagonist: {ToPrettyString(gameRule)}, {protoId}");
        _adminLogger.Add(LogType.AntagSelection, $"Pre-selected {player.Name} as antagonist: {ToPrettyString(gameRule)}, {protoId}");
    }

    /// <summary>
    /// Removes a player from pre-selection, this can occur naturally due to a player disconnecting or dying, or due to errors.
    /// This should only be called if a player cannot become antag, don't call this if a player becomes antag, we want that cached still.
    /// </summary>
    /// <param name="gameRule">Game rule which had chosen this player for antag, but failed to make them an antag.</param>
    /// <param name="protoId">Antag prototype this player didn't become.</param>
    /// <param name="player">Player.</param>
    private void DeSelectSession(Entity<AntagSelectionComponent> gameRule,
        ProtoId<AntagSpecifierPrototype> protoId,
        ICommonSession player)
    {
        if (!gameRule.Comp.PreSelectedSessions.TryGetValue(protoId, out var set))
        {
            Log.Error($"Attempted to remove {player.Name} from antag pre-selection, but the rule {protoId} hasn't been pre-selected!");
            return;
        }

        DeSelectSession(gameRule, protoId, player, set);
    }

    private void DeSelectSession(Entity<AntagSelectionComponent> gameRule,
        ProtoId<AntagSpecifierPrototype> protoId,
        ICommonSession player,
        HashSet<ICommonSession> set)
    {
        if (!set.Remove(player))
        {
            Log.Error($"Attempted to remove {player.Name} from antag pre-selection, but they weren't pre-selected in the first place!");
            return;
        }

        // Not an error because player could've disconnected or died or something.
        Log.Debug($"De-selected {player.Name} as antagonist: {ToPrettyString(gameRule)}, {protoId}");
        _adminLogger.Add(LogType.AntagSelection, $"De-selected {player.Name} as antagonist: {ToPrettyString(gameRule)}, {protoId}");
    }

    /// <summary>
    /// Attempts to initialize a valid antag entity for a player.
    /// Will de-select the player if they fail to initialize.
    /// </summary>
    /// <param name="gameRule">Game rule which is trying to create an antag right now!</param>
    /// <param name="prototype">Antag prototype the player is becoming.</param>
    /// <param name="player">Player.</param>
    /// <returns>True if the player initialized as the selected antag.</returns>
    private bool TryInitializeAntag(Entity<AntagSelectionComponent> gameRule,
        AntagSpecifierPrototype prototype,
        ICommonSession player)
    {
        _adminLogger.Add(LogType.AntagSelection, $"Start trying to make {session:player} become the antagonist: {ent:subject}");

        if (checkPref && !ValidAntagPreference(session, def.PrefRoles))
            return false;
        }

        // Re-check entity validity now that the player has spawned.
        // Pre-selection bypasses the blacklist (AttachedEntity was null at that time),
        // so we must verify here before applying antag components.
        if (!IsEntityValid(antagEnt.Value, prototype))
        {
            if (antagEnt.Value != player.AttachedEntity)
                QueueDel(antagEnt.Value);
            DeSelectSession(gameRule, prototype, player);
            return false;

        if (onlyPreSelect && session != null)
        {
            if (!ent.Comp.PreSelectedSessions.TryGetValue(def, out var set))
                ent.Comp.PreSelectedSessions.Add(def, set = new HashSet<ICommonSession>());
            set.Add(session);
            Log.Debug($"Pre-selected {session!.Name} as antagonist: {ToPrettyString(ent)}");
            _adminLogger.Add(LogType.AntagSelection, LogImpact.Medium, $"Pre-selected {session.Name} as antagonist: {ent.Owner:entity}");
        }
        else
        {
            MakeAntag(ent, session, def, ignoreSpawner);
        }

        InitializeAntag(gameRule, prototype, antagEnt.Value, player);
        return true;
    }

    private bool TryGetAntagEntity(Entity<AntagSelectionComponent> gameRule,
        AntagSpecifierPrototype prototype,
        ICommonSession player,
        [NotNullWhen(true)]out EntityUid? antagEnt)
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
            _adminLogger.Add(LogType.AntagSelection, $"Antag spawner {spawner:subject} in gamerule {ent:tool} failed due to not having {nameof(GhostRoleAntagSpawnerComponent)}.");
            Del(spawner);
            return null;
        }

        spawnerComp.Rule = ent;
        spawnerComp.Definition = def;
        return spawner;
    }

    /// <summary>
    /// Attempts to get an entity to assign antag to for a session.
    /// First by raising an event to see if the associated <see cref="gameRule"/> has an entity it wants to spawn,
    /// Then falling back to the attached entity for the player's session if the game rule doesn't have a specific entity.
    /// Private because it can create an entity, and it needs to be called with <see cref="InitializeAntag"/>
    /// </summary>
    /// <param name="gameRule">Associated game rule entity for our antag</param>
    /// <param name="prototype">Antag prototype we are trying to create</param>
    /// <param name="player">Player session we are making into an antag</param>
    /// <returns>Entity of the antagonist</returns>
    private EntityUid? GetAntagEntity(Entity<AntagSelectionComponent> gameRule,
        AntagSpecifierPrototype prototype,
        ICommonSession player)
    {
        // If there's no valid position for us to be moved to, then just return the entity currently attached to the session.
        // We need a position to spawn a new entity so we can't spawn a new entity without a proper position.
        // Doesn't throw an error since for some antags this is intended behavior.
        if (!TryGetValidSpawnPosition(gameRule, prototype, out var coordinates, player))
            return player.AttachedEntity;

        Log.Debug($"Pre-selected {session.Name} as antagonist: {ToPrettyString(ent)}");
        _adminLogger.Add(LogType.AntagSelection, $"Pre-selected {session:player} as antagonist: {ent:subject}");
    }

        if (player.AttachedEntity is not { } uid)
        {
            Log.Error($"Attempted to make {session} antagonist in gamerule {ToPrettyString(ent)} but there was no valid entity for player.");
            _adminLogger.Add(LogType.AntagSelection, LogImpact.High, $"Attempted to make {session} antagonist in gamerule {ent.Owner:entity} but there was no valid entity for player.");
            if (session != null && ent.Comp.RemoveUponFailedSpawn)
            {
                _adminLogger.Add(LogType.AntagSelection, $"Start trying to make {session} become the antagonist: {ToPrettyString(gameRule)}, {proto}");

            return;
        }

        // TODO: This is really messy because this part runs twice for midround events.
        // Once when the ghostrole spawner is created and once when a player takes it.
        // Therefore any component subscribing to this has to make sure both subscriptions return the same value
        // or the ghost role raffle location preview will be wrong.

        var getPosEv = new AntagSelectLocationEvent(session, ent, player);
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
                _adminLogger.Add(LogType.AntagSelection, LogImpact.High, $"Antag spawner {player:entity} in gamerule {ent.Owner:entity} failed due to not having GhostRoleAntagSpawnerComponent.");
                if (session != null)
                {
                    DeSelectSession(gameRule, proto, session, set);
                    SpawnGhostRole(gameRule, def);
                    continue;
                }

                TryInitializeAntag(gameRule, def, session);
            }
        }

        gameRule.Comp.AssignmentHandled = true;
    }

    /// <summary>
    /// Raises an event to the gamerule to check all valid possible spawning points for this rule.
    /// Returns a random spawnpoint from a list of valid spawnpoints, or null if there weren't any.
    /// </summary>
    private bool TryGetValidSpawnPosition(Entity<AntagSelectionComponent> ent, AntagSpecifierPrototype antag, [NotNullWhen(true)] out MapCoordinates? coordinates, ICommonSession? session = null)
    {
        coordinates = GetValidSpawnPosition(ent, antag, session);
        return coordinates != null;
    }

    /// <summary>
    /// Raises an event to the gamerule to check all valid possible spawning points for this rule.
    /// Returns a random spawnpoint from a list of valid spawnpoints, or null if there weren't any.
    /// </summary>
    private MapCoordinates? GetValidSpawnPosition(Entity<AntagSelectionComponent> ent, AntagSpecifierPrototype antag, ICommonSession? session = null)
    {
        var getPosEv = new AntagSelectLocationEvent(ent, antag, session);
        RaiseLocalEvent(ent, ref getPosEv, true);

        if (!getPosEv.Handled)
            return null;

        return RobustRandom.Pick(getPosEv.Coordinates);
    }

    /// <summary>
    /// Initializes the antagonist status on the specified entity.
    /// Adds the needed components, loadouts, items, attaches the player and fires off an event.
    /// </summary>
    private void InitializeAntag(Entity<AntagSelectionComponent> gameRule, AntagSpecifierPrototype prototype, EntityUid antag, ICommonSession player)
    {
        // Make sure player was properly pre-selected.
        Debug.Assert(gameRule.Comp.PreSelectedSessions.TryGetValue(prototype.ID, out var value) && value.Contains(player),
            $"Game rule {ToPrettyString(gameRule)}, failed to pre-assign {player.Name} to antag {prototype.ID}");

        // The following is where we apply components, equipment, and other changes to our antagonist entity.
        AssignAntagComponents(antag, prototype);

        // Equip the entity's RoleLoadout and LoadoutGroup
        List<ProtoId<StartingGearPrototype>> gear = new();
        if (prototype.StartingGear is not null)
            gear.Add(prototype.StartingGear.Value);

        _loadout.Equip(antag, gear, prototype.RoleLoadout);

        // Ensure that we have the right mind for our entity.
        if (!_mind.TryGetMind(player, out var mind, out var mindComp) || mindComp.OwnedEntity != antag)
            mind = _mind.CreateMind(player.UserId, Name(antag));

        _mind.TransferTo(mind, antag, ghostCheckOverride: true);
        _role.MindAddRoles(mind, prototype.MindRoles, silent: true);
        AssignMind(gameRule, prototype, mind, antag);

        Log.Debug($"Assigned {ToPrettyString(antag):target}, mind {ToPrettyString(mind):target} as antagonist: {ToPrettyString(gameRule):user}");
        _adminLogger.Add(LogType.AntagSelection, $"Assigned {ToPrettyString(antag):target}, mind {ToPrettyString(mind):target} as antagonist: {ToPrettyString(gameRule):user}");

            Log.Debug($"Assigned {ToPrettyString(curMind)} as antagonist: {ToPrettyString(ent)}");
            _adminLogger.Add(LogType.AntagSelection, $"Assigned {curMind:actor} as antagonist: {ent:subject}");
        }

        var afterEv = new AfterAntagEntitySelectedEvent(player, antag, gameRule, prototype);
        RaiseLocalEvent(gameRule, ref afterEv, true);
    }

    private void AssignMind(Entity<AntagSelectionComponent> gameRule, ProtoId<AntagSpecifierPrototype> proto, EntityUid mind, EntityUid antag)
    {
        if (gameRule.Comp.AssignedMinds.TryGetValue(proto, out var minds))
        {
            minds.Add((mind, Name(antag)));
        }
        else
        {
            var hashset = new HashSet<(EntityUid, string)>();
            hashset.Add((mind, Name(antag)));
            gameRule.Comp.AssignedMinds.Add(proto, hashset);
        }
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
[ByRefEvent]
public record struct AntagSelectEntityEvent(Entity<AntagSelectionComponent> GameRule, AntagSpecifierPrototype Antag, MapCoordinates Coords, ICommonSession? Session)
{
    public readonly ICommonSession? Session = Session;

    /// list of antag role prototypes associated with a entity. used by the <see cref="AntagMultipleRoleSpawnerComponent"/>
    public readonly AntagSpecifierPrototype Antag = Antag;

    public readonly MapCoordinates Coords = Coords;

    public bool Handled => Entity != null;

    public EntityUid? Entity;
}

/// <summary>
/// Event raised on a game rule entity to determine the location for the antagonist.
/// Methods responding to this event should not be making any changed as future methods can fail causing an antag to not spawn.
/// </summary>
[ByRefEvent]
public record struct AntagSelectLocationEvent(Entity<AntagSelectionComponent> GameRule, AntagSpecifierPrototype Antag, ICommonSession? Session = null)
{
    public readonly ICommonSession? Session = Session;

    public bool Handled => Coordinates.Any();

    // the entity of the antagonist
    public AntagSpecifierPrototype Antag = Antag;

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
/// <param name="Active">Whether or not this game rule is currently active, cached to avoid needless HasComps.</param>
/// <param name="Count">The number of specified antags left to ticket. This value does change as antags are assigned.</param>
public record struct AntagRule(Entity<AntagSelectionComponent> GameRule, AntagSpecifierPrototype Definition, bool Active, int Count)
{
    public static implicit operator AntagRule((Entity<AntagSelectionComponent> GameRule, AntagSpecifierPrototype Defintion, bool active) quad)
    {
        return new AntagRule(quad.GameRule, quad.Defintion, quad.active, 1);
    }

    public static implicit operator AntagRule((Entity<AntagSelectionComponent> GameRule, AntagSpecifierPrototype Defintion, bool active, int Count) quad)
    {
        return new AntagRule(quad.GameRule, quad.Defintion, quad.active, quad.Count);
    }
}

/// <summary>
/// A simple struct that stores an antag definition and the number of remaining slots available.
/// Typically, is paired with a <see cref="Entity{AntagSelectionComponent}"/> or else it's worthless.
/// </summary>
/// <param name="Definition">The antag definition we have a count of</param>
/// <param name="Count">The number of slots remaining for this antag</param>
public record struct AntagCount(AntagSpecifierPrototype Definition, int Count)
{
    /// <summary>
    /// Remaining number of slots for this antag.
    /// </summary>
    public int Count = Count;

    public static implicit operator AntagCount(AntagSpecifierPrototype definition)
    {
        return new AntagCount(definition, 1);
    }

    public static implicit operator AntagCount((AntagSpecifierPrototype Defintion, int Count) tuple)
    {
        return new AntagCount(tuple.Defintion, tuple.Count);
    }
}
