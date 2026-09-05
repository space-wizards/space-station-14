using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{
    /// <summary>
    /// Designated game rule that spawns a fake antagonist to discourage metagaming.
    /// Has to be a string since <see cref="EntProtoId"/> cannot be a const.
    /// </summary>
    public const string DummyGameRule = "DummyNonAntag";

    /// <summary>
    /// List of ignored game rules, these rules won't be spawned by normal means.
    /// This list is populated by <see cref="CCVars.GameTickerIgnoredPresets"/>
    /// </summary>
    [ViewVariables] private string[] _ignoredRules = [];

    [ViewVariables] protected readonly List<GameRule> AllRoundGameRules = [];

    [SubscribeLocalEvent]
    private void OnGameRuleAdded(Entity<GameRuleComponent> rule, ref MapInitEvent args)
    {
        var ev = new GameRuleAddedEvent(rule);
        RaiseLocalEvent(rule, ref ev, true);

        if (rule.Comp.Silent)
        {
            EndGameRule(rule.AsNullable());
            return;
        }

        AllRoundGameRules.Add((GetRoundTime(), rule));
    }

    [SubscribeLocalEvent]
    private void OnGameRuleStarted(Entity<ActiveGameRuleComponent> rule, ref MapInitEvent args)
    {
        if (MetaData(rule).EntityPrototype is not { } proto)
            return;

        var ruleComp = RuleQuery.Comp(rule);
        StartRuleCache(rule);

        Log.Info($"Started game rule {ToPrettyString(rule)}");
        Admin.Add(LogType.EventStarted, $"Started game rule {ToPrettyString(rule)}");

        var ev = new GameRuleStartedEvent((rule, ruleComp), proto.ID);
        RaiseLocalEvent(rule, ref ev, true);
    }

    [SubscribeLocalEvent]
    private void OnGameRuleEnded(Entity<GameRuleComponent> rule, ref ComponentShutdown args)
    {
        RemComp<ActiveGameRuleComponent>(rule);
        var ev = new GameRuleEndedEvent(rule);
        RaiseLocalEvent(rule, ref ev, true);
    }

    /// <summary>
    /// Tries to add a gamerule to the current round, but ignores any <see cref="_ignoredRules"/>
    /// </summary>
    /// <param name="gameRule">Game rule entity that we are trying to spawn</param>
    /// <param name="force">Forces the game rule to spawn regardless of if it's ignored or not</param>
    /// <returns>The entityUid of the spawned game rule, if it wasn't ignored.</returns>
    public Entity<GameRuleComponent>? AddGameRule([ForbidLiteral] EntProtoId gameRule, bool force = false)
    {
        if (!force && IsIgnored(gameRule))
            return null;

        return SpawnGameRule(gameRule);
    }

    /// <summary>
    /// Checks if this GameRule should be ignored before a spawning attempt.
    /// </summary>
    /// <param name="gameRule">GameRule we are trying to validate</param>
    /// <returns>True if the gamerule should be ignored and not spawned.</returns>
    public bool IsIgnored([ForbidLiteral] EntProtoId gameRule)
    {
        return _ignoredRules.Contains(gameRule);
    }

    /// <summary>
    /// Spawns a GameRule in nullspace! Protected since it ignores the rule blacklist!
    /// </summary>
    /// <param name="ruleId">Name of the game rule we're spawning</param>
    /// <returns>EntityUid of the rule we spawned.</returns>
    protected virtual Entity<GameRuleComponent> SpawnGameRule(EntProtoId ruleId)
    {
        var rule = Spawn(ruleId, MapCoordinates.Nullspace);
        var meta = MetaData(rule);
        Log.Info($"Added game rule {ToPrettyString((rule, meta))}");
        Admin.Add(LogType.EventStarted, $"Added game rule {ToPrettyString((rule, meta))}");

        // This should probably be a bool on the GameRuleComponent...
        if (RuleQuery.TryComp(rule, out var ruleComp))
            return (rule, ruleComp);

        Log.Error($"Entity {ToPrettyString((rule, meta))} lacked a {nameof(GameRuleComponent)}!");
        ruleComp = AddComp<GameRuleComponent>(rule);
        return (rule, ruleComp);
    }

    /// <summary>
    /// Game rules can be 'started' separately from being added. 'Starting' them usually
    /// happens at round start while they can be added and removed before then.
    /// </summary>
    [PublicAPI]
    public bool StartGameRule([ForbidLiteral] EntProtoId ruleId)
    {
        return StartGameRule(ruleId, out _);
    }

    /// <summary>
    /// Game rules can be 'started' separately from being added. 'Starting' them usually
    /// happens at round start while they can be added and removed before then.
    /// </summary>
    [PublicAPI]
    public bool StartGameRule([ForbidLiteral] EntProtoId ruleId, [NotNullWhen(true)] out Entity<GameRuleComponent>? ruleEntity)
    {
        ruleEntity = AddGameRule(ruleId);
        if (ruleEntity == null)
            return false;

        return StartGameRule(ruleEntity.Value.AsNullable()); // Worst shit I've ever seen in C#
    }

    /// <summary>
    /// Game rules can be 'started' separately from being added. 'Starting' them usually
    /// happens at round start while they can be added and removed before then.
    /// </summary>
    [PublicAPI]
    public bool StartGameRule(Entity<GameRuleComponent?> rule)
    {
        // Game rule has already ended itself, or this was never a game rule...
        if (!RuleQuery.Resolve(rule, ref rule.Comp, false))
            return false;

        DebugTools.Assert(!rule.Comp.Silent, $"Rule {ToPrettyString(rule)} attempted to start when it should have been ended!");

        // can't start an already active rule
        if (ActiveRuleQuery.HasComp(rule))
            return false;

        // TODO: Don't want to refactor this now, but this seems unnecessary and like it's hiding bugs...
        // If this rule has a delay, check if the delay was already applied. If it was, then start the rule now.
        if (rule.Comp.Delay == null || RemComp<DelayedStartRuleComponent>(rule))
        {
            AddComp<ActiveGameRuleComponent>(rule);
            rule.Comp.ActivatedAt = Timing.CurTime;
            return true;
        }

        var delayTime = TimeSpan.FromSeconds(rule.Comp.Delay.Value.Next(Random));

        if (delayTime <= TimeSpan.Zero)
            return true;

        Log.Info($"Queued start for game rule {ToPrettyString(rule)} with delay {delayTime}");
        Admin.Add(LogType.EventStarted,
            $"Queued start for game rule {ToPrettyString(rule)} with delay {delayTime}");

        var delayed = EnsureComp<DelayedStartRuleComponent>(rule);
        delayed.RuleStartTime = Timing.CurTime + (delayTime);
        return true;
    }

    private void StartRuleCache(EntityUid uid)
    {
        // Very likely to be a recently added rule, so we start from the top!
        for (var i = AllRoundGameRules.Count - 1; i >= 0; i--)
        {
            var rule = AllRoundGameRules[i];
            if (rule.Uid != uid)
                continue;

            if (!rule.StartRule(GetRoundTime()))
                Log.Error($"Rule {uid} tried to be started, but was already started!");

            AllRoundGameRules[i] = rule;
            return;
        }

        Log.Error($"Rule {ToPrettyString(uid)} was started but had not been added yet somehow!");
        AllRoundGameRules.Add((GetRoundTime(), uid, GameRuleLifeStage.Started));
    }

    /// <summary>
    /// Ends a game rule.
    /// </summary>
    [PublicAPI]
    public bool EndGameRule(Entity<GameRuleComponent?> rule)
    {
        // Don't log missing because we could've already ended the rule.
        // TODO: Maybe do log missing to ensure we only ever try to end a rule once?
        if (!Resolve(rule, ref rule.Comp, false))
            return false;

        RemComp(rule, rule.Comp);

        Log.Info($"Ended game rule {ToPrettyString(rule)}");
        Admin.Add(LogType.EventStopped, $"Ended game rule {ToPrettyString(rule)}");
        return true;
    }

    /// <summary>
    ///     Returns true if a game rule with the given component has been added.
    /// </summary>
    [PublicAPI]
    public bool IsGameRuleAdded<T>()
        where T : IComponent
    {
        var query = EntityQueryEnumerator<T, GameRuleComponent>();
        return query.MoveNext(out _, out _, out _);
    }

    [PublicAPI]
    public bool IsGameRuleAdded(Entity<GameRuleComponent?> rule)
    {
        return Resolve(rule, ref rule.Comp);
    }

    [PublicAPI]
    public bool IsGameRuleAdded([ForbidLiteral] string rule)
    {
        foreach (var ruleEntity in GetAddedGameRules())
        {
            if (MetaData(ruleEntity).EntityPrototype?.ID == rule)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if a game rule that passes the whitelist and blacklist has been added.
    /// </summary>
    /// <param name="ruleWhitelist">whitelist for the game rules</param>
    /// <param name="ruleBlacklist">blacklist for the game rules</param>
    [PublicAPI]
    public bool IsGameRuleAdded(EntityWhitelist? ruleWhitelist, EntityWhitelist? ruleBlacklist = null)
    {
        foreach (var ruleEntity in GetAddedGameRules())
        {
            if (Whitelist.CheckBoth(ruleEntity, ruleBlacklist, ruleWhitelist))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Returns true if a game rule with the given component is active..
    /// </summary>
    [PublicAPI]
    public bool IsGameRuleActive<T>()
        where T : IComponent
    {
        var query = EntityQueryEnumerator<T, ActiveGameRuleComponent, GameRuleComponent>();
        // out, damned underscore!!!
        while (query.MoveNext(out _, out _, out _, out _))
        {
            return true;
        }

        return false;
    }

    [PublicAPI]
    public bool IsGameRuleActive(Entity<GameRuleComponent?> entity)
    {
        return Resolve(entity, ref entity.Comp) && HasComp<ActiveGameRuleComponent>(entity);
    }

    [PublicAPI]
    public bool IsGameRuleActive([ForbidLiteral] string rule)
    {
        foreach (var ruleEntity in GetActiveGameRules())
        {
            if (MetaData(ruleEntity).EntityPrototype?.ID == rule)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if a game rule that passes the whitelist and blacklist is active.
    /// </summary>
    /// <param name="ruleWhitelist">whitelist for the game rules</param>
    /// <param name="ruleBlacklist">blacklist for the game rules</param>
    [PublicAPI]
    public bool IsGameRuleActive(EntityWhitelist? ruleWhitelist, EntityWhitelist? ruleBlacklist = null)
    {
        foreach (var ruleEntity in GetActiveGameRules())
        {
            if (Whitelist.CheckBoth(ruleEntity, ruleBlacklist, ruleWhitelist))
                return true;
        }

        return false;
    }


    /// <summary>
    /// Gets all the gamerule entities that have been added.
    /// </summary>
    [PublicAPI]
    public IEnumerable<EntityUid> GetAddedGameRules()
    {
        var query = EntityQueryEnumerator<GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleData))
        {
            if (IsGameRuleAdded((uid, ruleData)))
                yield return uid;
        }
    }

    /// <summary>
    /// Gets all the game rule entities which have not ended and match the given prototype.
    /// </summary>
    [PublicAPI]
    public IEnumerable<EntityUid> GetAddedGameRules(EntProtoId stationEvent)
    {
        var query = EntityQueryEnumerator<GameRuleComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var ruleData, out var meta))
        {
            if (IsGameRuleAdded((uid, ruleData)) && meta.EntityPrototype?.Name is { } id && id == stationEvent)
                yield return uid;
        }
    }

    /// <summary>
    /// Gets all the gamerule entities with {T} component that have been added.
    /// </summary>
    [PublicAPI]
    public IEnumerable<Entity<T>> GetAddedGameRules<T>() where T : Component
    {
        var query = EntityQueryEnumerator<T, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var ruleData))
        {
            if (IsGameRuleAdded((uid, ruleData)))
                yield return (uid, comp);
        }
    }

    /// <summary>
    /// Gets all the game rule entities with {T} which have not ended and match the given prototype.
    /// </summary>
    [PublicAPI]
    public IEnumerable<Entity<T>> GetAddedGameRules<T>(EntProtoId stationEvent) where T : Component
    {
        var query = EntityQueryEnumerator<T, GameRuleComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var ruleData, out var meta))
        {
            if (IsGameRuleAdded((uid, ruleData)) && meta.EntityPrototype?.Name is { } id && id == stationEvent)
                yield return (uid, comp);
        }
    }

    /// <summary>
    /// Gets all the gamerule entities which are currently active.
    /// </summary>
    [PublicAPI]
    public IEnumerable<EntityUid> GetActiveGameRules()
    {
        var query = EntityQueryEnumerator<ActiveGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            yield return uid;
        }
    }

    /// <summary>
    /// Gets all the game rule entities which are currently active and match the given prototype.
    /// </summary>
    [PublicAPI]
    public IEnumerable<EntityUid> GetActiveGameRules(EntProtoId stationEvent)
    {
        var query = EntityQueryEnumerator<ActiveGameRuleComponent, GameRuleComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var meta))
        {
            if (meta.EntityPrototype?.Name is { } id && id == stationEvent)
                yield return uid;
        }
    }

    /// <summary>
    /// Gets all the gamerule entities with {T} component that are currently active.
    /// </summary>
    [PublicAPI]
    public IEnumerable<Entity<T>> GetActiveGameRules<T>() where T : Component
    {
        var query = EntityQueryEnumerator<T, ActiveGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out _, out _))
        {
            yield return (uid, comp);
        }
    }

    /// <summary>
    /// Gets all the game rule entities with {T} component that are currently active and have the given prototype.
    /// </summary>
    [PublicAPI]
    public IEnumerable<Entity<T>> GetActiveGameRules<T>(EntProtoId stationEvent) where T : Component
    {
        var query = EntityQueryEnumerator<T, ActiveGameRuleComponent, GameRuleComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out _, out _, out var meta))
        {
            if (meta.EntityPrototype?.Name is { } id && id == stationEvent)
                yield return (uid, comp);
        }
    }

    /// <summary>
    /// Gets all gamerule prototypes
    /// </summary>
    [PublicAPI]
    public IEnumerable<EntityPrototype> GetAllGameRulePrototypes()
    {
        foreach (var proto in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract)
                continue;

            if (HasComp<GameRuleComponent>(proto))
                yield return proto;
        }
    }


    [PublicAPI]
    public int GetOccurrences(EntityPrototype stationEvent)
    {
        return GetOccurrences(stationEvent.ID);
    }

    /// <summary>
    /// Gets the total number of game rule entities which match the inputted prototype!
    /// </summary>
    /// <param name="stationEvent">Event prototype we are looking for.</param>
    /// <returns>Number of already existing events which match this prototype.</returns>
    [PublicAPI]
    public int GetOccurrences(EntProtoId stationEvent)
    {
        var count = 0;
        var ruleQuery = EntityQueryEnumerator<GameRuleComponent, MetaDataComponent>();
        while (ruleQuery.MoveNext(out _, out var meta))
        {
            if (meta.EntityPrototype?.Name is { } id&& id == stationEvent)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Gets the last added game rule or null if there are no game rules!
    /// </summary>
    [PublicAPI]
    public GameRule? GetLastGameRule()
    {
        if (AllRoundGameRules.Count == 0)
            return null;

        return AllRoundGameRules[^1];
    }

    /// <summary>
    /// Gets the last added game rule or null if there are no game rules!
    /// </summary>
    [PublicAPI]
    public TimeSpan GetLastRuleTime()
    {
        return GetLastGameRule() is not { } rule ? TimeSpan.Zero : rule.StartTime;
    }

    /// <summary>
    /// Gets the last added game rule or null if there are no game rules!
    /// </summary>
    [PublicAPI]
    public GameRule? GetLastGameRule(EntProtoId proto)
    {
        for (var i = AllRoundGameRules.Count; i >= 0; i--)
        {
            var rule = AllRoundGameRules[i];
            if (Deleted(rule.Uid) || MetaData(rule.Uid).EntityPrototype?.ID != proto.Id)
                continue;

            return rule;
        }

        return null;
    }

    /// <summary>
    /// Gets the last added game rule or null if there are no game rules!
    /// </summary>
    [PublicAPI]
    public TimeSpan GetLastRuleTime(EntProtoId proto)
    {
        return GetLastGameRule(proto) is not { } rule ? TimeSpan.Zero : rule.StartTime;
    }

    /// <summary>
    /// Returns a readable string for this Game Rule.
    /// </summary>
    /// <param name="rule">GameRule struct we're searching for.</param>
    /// <returns>A readable string representing this gamerule and its status.</returns>
    [PublicAPI]
    public string RuleToString(GameRule rule)
    {
        return RuleToString(rule.Uid, rule.LifeStage);
    }

    /// <inheritdoc cref="RuleToString(GameRule)"/>
    [PublicAPI]
    public string RuleToString(EntityUid rule, GameRuleLifeStage stage)
    {
        if (!TryComp(rule, out MetaDataComponent? meta))
            return "Deleted Rule";

        var str = $"{meta.EntityPrototype?.ID ?? "Unknown Rule"} ({rule.Id})";
        switch (stage)
        {
            case GameRuleLifeStage.Added:
                str += " - Pending";
                break;
            case GameRuleLifeStage.Ended:
                str += " - Ended";
                break;
        }

        return str;
    }
}
