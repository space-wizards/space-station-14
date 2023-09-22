using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Administration;
using Content.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [ViewVariables] private readonly List<(TimeSpan, string)> _allPreviousGameRules = new();

    /// <summary>
    ///     A list storing the start times of all game rules that have been started this round.
    ///     Game rules can be started and stopped at any time, including midround.
    /// </summary>
    public IReadOnlyList<(TimeSpan, string)> AllPreviousGameRules => _allPreviousGameRules;

    private void InitializeGameRules()
    {
        // Add game rule command.
        _consoleHost.RegisterCommand("addgamerule",
            string.Empty,
            "addgamerule <rules>",
            AddGameRuleCommand,
            AddGameRuleCompletions);

        // End game rule command.
        _consoleHost.RegisterCommand("endgamerule",
            string.Empty,
            "endgamerule <rules>",
            EndGameRuleCommand,
            EndGameRuleCompletions);

        // Clear game rules command.
        _consoleHost.RegisterCommand("cleargamerules",
            string.Empty,
            "cleargamerules",
            ClearGameRulesCommand);
    }

    private void ShutdownGameRules()
    {
        _consoleHost.UnregisterCommand("addgamerule");
        _consoleHost.UnregisterCommand("endgamerule");
        _consoleHost.UnregisterCommand("cleargamerules");
    }

    /// <summary>
    /// Adds a game rule to the list, but does not
    /// start it yet, instead waiting until the rule is actually started by other code (usually roundstart)
    /// </summary>
    /// <returns>The entity for the added gamerule</returns>
    public EntityUid AddGameRule(string ruleId)
    {
        var ruleEntity = Spawn(ruleId, MapCoordinates.Nullspace);
        _sawmill.Info($"Added game rule {ToPrettyString(ruleEntity)}");

        var ev = new GameRuleAddedEvent(ruleEntity, ruleId);
        RaiseLocalEvent(ruleEntity, ref ev, true);
        return ruleEntity;
    }

    /// <summary>
    /// Game rules can be 'started' separately from being added. 'Starting' them usually
    /// happens at round start while they can be added and removed before then.
    /// </summary>
    public bool StartGameRule(string ruleId)
    {
        return StartGameRule(ruleId, out _);
    }

    /// <summary>
    /// Game rules can be 'started' separately from being added. 'Starting' them usually
    /// happens at round start while they can be added and removed before then.
    /// </summary>
    public bool StartGameRule(string ruleId, out EntityUid ruleEntity)
    {
        ruleEntity = AddGameRule(ruleId);
        return StartGameRule(ruleEntity);
    }

    /// <summary>
    /// Game rules can be 'started' separately from being added. 'Starting' them usually
    /// happens at round start while they can be added and removed before then.
    /// </summary>
    public bool StartGameRule(EntityUid ruleEntity, GameRuleComponent? ruleData = null)
    {
        if (!Resolve(ruleEntity, ref ruleData))
            ruleData ??= EnsureComp<GameRuleComponent>(ruleEntity);

        // can't start an already active rule
        if (ruleData.Active || ruleData.Ended)
            return false;

        if (MetaData(ruleEntity).EntityPrototype?.ID is not { } id) // you really fucked up
            return false;

        _allPreviousGameRules.Add((RoundDuration(), id));
        _sawmill.Info($"Started game rule {ToPrettyString(ruleEntity)}");

        ruleData.Active = true;
        ruleData.ActivatedAt = _gameTiming.CurTime;
        var ev = new GameRuleStartedEvent(ruleEntity, id);
        RaiseLocalEvent(ruleEntity, ref ev, true);
        return true;
    }

    /// <summary>
    /// Ends a game rule.
    /// </summary>
    [PublicAPI]
    public bool EndGameRule(EntityUid ruleEntity, GameRuleComponent? ruleData = null)
    {
        if (!Resolve(ruleEntity, ref ruleData))
            return false;

        // don't end it multiple times
        if (ruleData.Ended)
            return false;

        if (MetaData(ruleEntity).EntityPrototype?.ID is not { } id) // you really fucked up
            return false;

        ruleData.Active = false;
        ruleData.Ended = true;
        _sawmill.Info($"Ended game rule {ToPrettyString(ruleEntity)}");

        var ev = new GameRuleEndedEvent(ruleEntity, id);
        RaiseLocalEvent(ruleEntity, ref ev, true);
        return true;
    }

    public bool IsGameRuleAdded(EntityUid ruleEntity, GameRuleComponent? component = null)
    {
        return Resolve(ruleEntity, ref component) && !component.Ended;
    }

    public bool IsGameRuleAdded(string rule)
    {
        foreach (var ruleEntity in GetAddedGameRules())
        {
            if (MetaData(ruleEntity).EntityPrototype?.ID == rule)
                return true;
        }

        return false;
    }

    public bool IsGameRuleActive(EntityUid ruleEntity, GameRuleComponent? component = null)
    {
        return Resolve(ruleEntity, ref component) && component.Active;
    }

    public bool IsGameRuleActive(string rule)
    {
        foreach (var ruleEntity in GetActiveGameRules())
        {
            if (MetaData(ruleEntity).EntityPrototype?.ID == rule)
                return true;
        }

        return false;
    }

    public void ClearGameRules()
    {
        foreach (var rule in GetAddedGameRules())
        {
            EndGameRule(rule);
        }
    }

    /// <summary>
    /// Gets all the gamerule entities which are currently active.
    /// </summary>
    public IEnumerable<EntityUid> GetAddedGameRules()
    {
        var query = EntityQueryEnumerator<GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleData))
        {
            if (IsGameRuleAdded(uid, ruleData))
                yield return uid;
        }
    }

    /// <summary>
    /// Gets all the gamerule entities which are currently active.
    /// </summary>
    public IEnumerable<EntityUid> GetActiveGameRules()
    {
        var query = EntityQueryEnumerator<GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleData))
        {
            if (ruleData.Active)
                yield return uid;
        }
    }

    /// <summary>
    /// Gets all gamerule prototypes
    /// </summary>
    public IEnumerable<EntityPrototype> GetAllGameRulePrototypes()
    {
        foreach (var proto in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract)
                continue;

            if (proto.HasComponent<GameRuleComponent>())
                yield return proto;
        }
    }

    #region Command Implementations

    [AdminCommand(AdminFlags.Fun)]
    private void AddGameRuleCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length == 0)
            return;

        foreach (var rule in args)
        {
            var ent = AddGameRule(rule);

            // Start rule if we're already in the middle of a round
            if(RunLevel == GameRunLevel.InRound)
                StartGameRule(ent);
        }
    }

    private CompletionResult AddGameRuleCompletions(IConsoleShell shell, string[] args)
    {
        return CompletionResult.FromHintOptions(GetAllGameRulePrototypes().Select(p => p.ID), "<rule>");
    }

    [AdminCommand(AdminFlags.Fun)]
    private void EndGameRuleCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length == 0)
            return;

        foreach (var rule in args)
        {
            if (!NetEntity.TryParse(rule, out var ruleEntNet) || !TryGetEntity(ruleEntNet, out var ruleEnt))
                continue;

            EndGameRule(ruleEnt.Value);
        }
    }

    private CompletionResult EndGameRuleCompletions(IConsoleShell shell, string[] args)
    {
        var opts = GetAddedGameRules().Select(ent => new CompletionOption(ent.ToString(), ToPrettyString(ent))).ToList();
        return CompletionResult.FromHintOptions(opts, "<added rule>");
    }

    [AdminCommand(AdminFlags.Fun)]
    private void ClearGameRulesCommand(IConsoleShell shell, string argstr, string[] args)
    {
        ClearGameRules();
    }

    #endregion
}

/*
/// <summary>
///     Raised broadcast when a game rule is selected, but not started yet.
/// </summary>
public sealed class GameRuleAddedEvent
{
    public GameRulePrototype Rule { get; }

    public GameRuleAddedEvent(GameRulePrototype rule)
    {
        Rule = rule;
    }
}

public sealed class GameRuleStartedEvent
{
    public GameRulePrototype Rule { get; }

    public GameRuleStartedEvent(GameRulePrototype rule)
    {
        Rule = rule;
    }
}

public sealed class GameRuleEndedEvent
{
    public GameRulePrototype Rule { get; }

    public GameRuleEndedEvent(GameRulePrototype rule)
    {
        Rule = rule;
    }
}
*/
