using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Localization;

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

        // List game rules command.
        var localizedHelp = Loc.GetString("listgamerules-command-help");

        _consoleHost.RegisterCommand("listgamerules",
            string.Empty,
            $"listgamerules - {localizedHelp}",
            ListGameRuleCommand);
    }

    private void ShutdownGameRules()
    {
        _consoleHost.UnregisterCommand("addgamerule");
        _consoleHost.UnregisterCommand("endgamerule");
        _consoleHost.UnregisterCommand("cleargamerules");
        _consoleHost.UnregisterCommand("listgamerules");
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
        _adminLogger.Add(LogType.EventStarted, $"Added game rule {ToPrettyString(ruleEntity)}");
        var str = Loc.GetString("station-event-system-run-event", ("eventName", ToPrettyString(ruleEntity)));
#if DEBUG
        _chatManager.SendAdminAlert(str);
#else
        if (RunLevel == GameRunLevel.InRound) // avoids telling admins the round type before it starts so that can be handled elsewhere.
        {
            _chatManager.SendAdminAlert(str);
        }
#endif
        Log.Info(str);

        var ev = new GameRuleAddedEvent(ruleEntity, ruleId);
        RaiseLocalEvent(ruleEntity, ref ev, true);

        var currentTime = RunLevel == GameRunLevel.PreRoundLobby ? TimeSpan.Zero : RoundDuration();
        if (!HasComp<RoundstartStationVariationRuleComponent>(ruleEntity) && !HasComp<StationVariationPassRuleComponent>(ruleEntity))
        {
            _allPreviousGameRules.Add((currentTime, ruleId + " (Pending)"));
        }

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
        if (HasComp<ActiveGameRuleComponent>(ruleEntity) || HasComp<EndedGameRuleComponent>(ruleEntity))
            return false;

        if (MetaData(ruleEntity).EntityPrototype?.ID is not { } id) // you really fucked up
            return false;

        // If we already have it, then we just skip the delay as it has already happened.
        if (!RemComp<DelayedStartRuleComponent>(ruleEntity) && ruleData.Delay != null)
        {
            var delayTime = TimeSpan.FromSeconds(ruleData.Delay.Value.Next(_robustRandom));

            if (delayTime > TimeSpan.Zero)
            {
                _sawmill.Info($"Queued start for game rule {ToPrettyString(ruleEntity)} with delay {delayTime}");
                _adminLogger.Add(LogType.EventStarted,
                    $"Queued start for game rule {ToPrettyString(ruleEntity)} with delay {delayTime}");

                var delayed = EnsureComp<DelayedStartRuleComponent>(ruleEntity);
                delayed.RuleStartTime = _gameTiming.CurTime + (delayTime);
                return true;
            }
        }

        var currentTime = RunLevel == GameRunLevel.PreRoundLobby ? TimeSpan.Zero : RoundDuration();

        // Remove the first occurrence of the pending entry before adding the started entry
        var pendingRuleIndex = _allPreviousGameRules.FindIndex(rule => rule.Item2 == id + " (Pending)");
        if (pendingRuleIndex >= 0)
        {
            _allPreviousGameRules.RemoveAt(pendingRuleIndex);
        }

        if (!HasComp<RoundstartStationVariationRuleComponent>(ruleEntity) && !HasComp<StationVariationPassRuleComponent>(ruleEntity))
        {
            _allPreviousGameRules.Add((currentTime, id));
        }

        _sawmill.Info($"Started game rule {ToPrettyString(ruleEntity)}");
        _adminLogger.Add(LogType.EventStarted, $"Started game rule {ToPrettyString(ruleEntity)}");

        EnsureComp<ActiveGameRuleComponent>(ruleEntity);
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
        if (HasComp<EndedGameRuleComponent>(ruleEntity))
            return false;

        if (MetaData(ruleEntity).EntityPrototype?.ID is not { } id) // you really fucked up
            return false;

        RemComp<ActiveGameRuleComponent>(ruleEntity);
        EnsureComp<EndedGameRuleComponent>(ruleEntity);

        _sawmill.Info($"Ended game rule {ToPrettyString(ruleEntity)}");
        _adminLogger.Add(LogType.EventStopped, $"Ended game rule {ToPrettyString(ruleEntity)}");

        var ev = new GameRuleEndedEvent(ruleEntity, id);
        RaiseLocalEvent(ruleEntity, ref ev, true);
        return true;
    }

    /// <summary>
    ///     Returns true if a game rule with the given component has been added.
    /// </summary>
    public bool IsGameRuleAdded<T>()
        where T : IComponent
    {
        var query = EntityQueryEnumerator<T, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            if (HasComp<EndedGameRuleComponent>(uid))
                continue;

            return true;
        }

        return false;
    }

    public bool IsGameRuleAdded(EntityUid ruleEntity, GameRuleComponent? component = null)
    {
        return Resolve(ruleEntity, ref component) && !HasComp<EndedGameRuleComponent>(ruleEntity);
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

    /// <summary>
    ///     Returns true if a game rule with the given component is active..
    /// </summary>
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

    public bool IsGameRuleActive(EntityUid ruleEntity, GameRuleComponent? component = null)
    {
        return Resolve(ruleEntity, ref component) && HasComp<ActiveGameRuleComponent>(ruleEntity);
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
        var query = EntityQueryEnumerator<ActiveGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
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

    private void UpdateGameRules()
    {
        var query = EntityQueryEnumerator<DelayedStartRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var delay, out var rule))
        {
            if (_gameTiming.CurTime < delay.RuleStartTime)
                continue;

            StartGameRule(uid, rule);
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
            if (!_prototypeManager.HasIndex(rule))
            {
                shell.WriteError($"Invalid game rule {rule} was skipped.");

                continue;
            }

            if (shell.Player != null)
            {
                _adminLogger.Add(LogType.EventStarted, $"{shell.Player} tried to add game rule [{rule}] via command");
                _chatManager.SendAdminAnnouncement(Loc.GetString("add-gamerule-admin", ("rule", rule), ("admin", shell.Player)));
            }
            else
            {
                _adminLogger.Add(LogType.EventStarted, $"Unknown tried to add game rule [{rule}] via command");
            }
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
            if (shell.Player != null)
            {
                _adminLogger.Add(LogType.EventStopped, $"{shell.Player} tried to end game rule [{rule}] via command");
            }
            else
            {
                _adminLogger.Add(LogType.EventStopped, $"Unknown tried to end game rule [{rule}] via command");
            }

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

    [AdminCommand(AdminFlags.Admin)]
    private void ListGameRuleCommand(IConsoleShell shell, string argstr, string[] args)
    {
        _sawmill.Info($"{shell.Player} tried to get list of game rules via command");
        _adminLogger.Add(LogType.Action, $"{shell.Player} tried to get list of game rules via command");
        var message = GetGameRulesListMessage(false);
        shell.WriteLine(message);
    }
    private string GetGameRulesListMessage(bool forChatWindow)
    {
        if (_allPreviousGameRules.Count > 0)
        {
            var sortedRules = _allPreviousGameRules.OrderBy(rule => rule.Item1).ToList();
            var message = "\n";

            if (!forChatWindow)
            {
                var header = Loc.GetString("list-gamerule-admin-header");
                message += $"\n{header}\n";
                message += "|------------|------------------\n";
            }

            foreach (var (time, rule) in sortedRules)
            {
                var formattedTime = time.ToString(@"hh\:mm\:ss");
                message += $"| {formattedTime,-10} | {rule,-16} \n";
            }

            return message;
        }
        else
        {
            return Loc.GetString("list-gamerule-admin-no-rules");

        }
    }

    #endregion
}
