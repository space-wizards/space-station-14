using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
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

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
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
    protected override Entity<GameRuleComponent> SpawnGameRule(EntProtoId ruleId)
    {
        var ruleEntity = base.SpawnGameRule(ruleId);
        var str = Loc.GetString("station-event-system-run-event", ("eventName", ToPrettyString(ruleEntity)));
#if DEBUG
        _chatManager.SendAdminAlert(str);
#else
        if (RunLevel == GameRunLevel.InRound) // avoids telling admins the round type before it starts so that can be handled elsewhere.
            _chatManager.SendAdminAlert(str);
#endif
        Log.Info(str);

        return ruleEntity;
    }

    public void ClearGameRules()
    {
        foreach (var rule in GetAddedGameRules())
        {
            EndGameRule(rule);
        }
    }

    private void UpdateGameRules()
    {
        var query = EntityQueryEnumerator<DelayedStartRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var delay, out var rule))
        {
            if (Timing.CurTime < delay.RuleStartTime)
                continue;

            StartGameRule((uid, rule));
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent args)
    {
        if (args.Forced || args.Cancelled)
            return;

        var query = EntityQueryEnumerator<GameRuleComponent>();
        while (query.MoveNext(out var uid, out var gameRule))
        {
            var minPlayers = gameRule.MinPlayers;
            var name = ToPrettyString(uid);

            if (args.Players.Length >= minPlayers)
                continue;

            if (gameRule.CancelPresetOnTooFewPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("preset-not-enough-ready-players",
                    ("readyPlayersCount", args.Players.Length),
                    ("minimumPlayers", minPlayers),
                    ("presetName", name)));
                args.Cancel();
                //TODO remove this once announcements are logged
                Log.Info($"Rule '{name}' requires {minPlayers} players, but only {args.Players.Length} are ready.");
            }
            else
            {
                EndGameRule((uid, gameRule));
            }
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
            if (!ProtoMan.HasIndex(rule))
            {
                shell.WriteError($"Invalid game rule {rule} was skipped.");

                continue;
            }

            if (shell.Player != null)
            {
                Admin.Add(LogType.EventStarted, $"{shell.Player} tried to add game rule [{rule}] via command");
                _chatManager.SendAdminAnnouncement(Loc.GetString("add-gamerule-admin", ("rule", rule), ("admin", shell.Player)));
            }
            else
            {
                Admin.Add(LogType.EventStarted, $"Unknown tried to add game rule [{rule}] via command");
            }
            var ent = SpawnGameRule(rule);

            // Start rule if we're already in the middle of a round
            // TODO: DO WE EVEN NEED THIS CHECK???
            if (RunLevel == GameRunLevel.InRound)
                StartGameRule(ent.AsNullable());
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
                Admin.Add(LogType.EventStopped, $"{shell.Player} tried to end game rule [{rule}] via command");
            }
            else
            {
                Admin.Add(LogType.EventStopped, $"Unknown tried to end game rule [{rule}] via command");
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
        Log.Info($"{shell.Player} tried to get list of game rules via command");
        Admin.Add(LogType.Action, $"{shell.Player} tried to get list of game rules via command");
        var message = GetGameRulesListMessage(false);
        shell.WriteLine(message);
    }

    private string GetGameRulesListMessage(bool forChatWindow)
    {
        if (AllRoundGameRules.Count > 0)
        {
            var message = new StringBuilder();
            message.AppendLine();

            if (!forChatWindow)
            {
                var header = Loc.GetString("list-gamerule-admin-header");
                message.AppendLine();
                message.AppendLine(header);
                message.AppendLine("|------------|---------------------------");
            }

            foreach (var (time, rule, stage) in AllRoundGameRules)
            {
                var formattedTime = time.ToString(@"hh\:mm\:ss");
                var name = RuleToString(rule, stage);
                message.AppendLine($"| {formattedTime,-10} | {name,-24} ");
            }

            return message.ToString().TrimEnd('\n');
        }

        return Loc.GetString("list-gamerule-admin-no-rules");
    }

    #endregion
}
