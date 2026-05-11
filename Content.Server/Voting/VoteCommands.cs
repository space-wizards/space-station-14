using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Discord.WebhookMessages;
using Content.Server.Voting.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Voting;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server.Voting
{
    [AnyCommand]
    public sealed partial class CreateVoteCommand : LocalizedEntityCommands
    {
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private IVoteManager _voteManager = default!;

        public override string Command => "createvote";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1 && args[0] != StandardVoteType.Votekick.ToString())
            {
                shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
                return;
            }
            if (args.Length != 3 && args[0] == StandardVoteType.Votekick.ToString())
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 3), ("currentAmount", args.Length)));
                return;
            }


            if (!Enum.TryParse<StandardVoteType>(args[0], ignoreCase: true, out var type))
            {
                shell.WriteError(Loc.GetString("cmd-createvote-invalid-vote-type"));
                return;
            }

            if (shell.Player != null && !_voteManager.CanCallVote(shell.Player, type))
            {
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{shell.Player} failed to start {type.ToString()} vote");
                shell.WriteError(Loc.GetString("cmd-createvote-cannot-call-vote-now"));
                return;
            }

            _voteManager.CreateStandardVote(shell.Player, type, args.Skip(1).ToArray());
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = Enum.GetNames<StandardVoteType>();
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-createvote-arg-vote-type"));
            }

            return CompletionResult.Empty;
        }
    }

    [AdminCommand(AdminFlags.Round)]
    public sealed partial class CreateCustomCommand : LocalizedEntityCommands
    {
        [Dependency] private IVoteManager _voteManager = default!;
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private IChatManager _chatManager = default!;
        [Dependency] private VoteWebhooks _voteWebhooks = default!;
        [Dependency] private IConfigurationManager _cfg = default!;

        // TODO: move this somewhere else to remove duplicated code
        public void StartCustomVote(ICommonSession? initiator, bool showResultsInChat, string title, List<string> options)
        {
            if (options.Count is < 2 or > 9)
                return;

            var voteOptions = new VoteOptions
            {
                Title = title,
                Duration = TimeSpan.FromSeconds(30),
                VoterEligibility = VoteManager.VoterEligibility.All,
            };

            for (var i = 1; i <= options.Count; i++)
            {
                voteOptions.Options.Add((options[i-1], i));
            }

            voteOptions.SetInitiatorOrServer(initiator);

            if (initiator != null)
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{initiator} initiated a custom vote: {voteOptions.Title} - {string.Join("; ", voteOptions.Options.Select(x => x.text))}");
            else
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Initiated a custom vote: {voteOptions.Title} - {string.Join("; ", voteOptions.Options.Select(x => x.text))}");

            var vote = _voteManager.CreateVote(voteOptions);

            var webhookState = _voteWebhooks.CreateWebhookIfConfigured(voteOptions, _cfg.GetCVar(CCVars.DiscordVoteWebhook));

            vote.OnFinished += (_, eventArgs) =>
            {
                if (eventArgs.Winner == null)
                {
                    var ties = string.Join(", ", eventArgs.Winners.Select(c => options[(int) c]));
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {voteOptions.Title} finished as tie: {ties}");

                    if (showResultsInChat)
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-tie", ("title", voteOptions.Title), ("ties", ties)));
                }
                else
                {
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {voteOptions.Title} finished: {options[(int) eventArgs.Winner]}");

                    if (showResultsInChat)
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-win", ("title", voteOptions.Title), ("winner", options[(int) eventArgs.Winner])));
                }

                _voteWebhooks.UpdateWebhookIfConfigured(webhookState, eventArgs);
            };

            vote.OnCancelled += _ =>
            {
                _voteWebhooks.UpdateCancelledWebhookIfConfigured(webhookState);
            };
        }

        private const int MaxArgCount = 10;

        public override string Command => "customvote";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is < 3 or > MaxArgCount)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 3), ("upper", 10)));
                return;
            }

            var options = new List<string>();
            for (var i = 1; i < args.Length; i++)
            {
                options.Add(args[i]);
            }

            StartCustomVote(shell.Player, true, args[0], options);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHint(Loc.GetString($"cmd-{Command}-arg-title"));

            if (args.Length > MaxArgCount)
                return CompletionResult.Empty;

            var n = args.Length - 1;
            return CompletionResult.FromHint(Loc.GetString($"cmd-{Command}-arg-option-n", ("n", n)));
        }
    }

    [ToolshedCommand, AdminCommand(AdminFlags.Admin)]
    public sealed partial class CustomVoteCommand : ToolshedCommand
    {
        [Dependency] private IVoteManager _voteManager = default!;
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private IChatManager _chatManager = default!;
        [Dependency] private VoteWebhooks _voteWebhooks = default!;
        [Dependency] private IConfigurationManager _cfg = default!;

        // TODO: move this somewhere else to remove duplicated code
        public void StartCustomVote(ICommonSession? initiator, bool showResultsInChat, string title, List<string> options)
        {
            if (options.Count is < 2 or > 9)
                return;

            var voteOptions = new VoteOptions
            {
                Title = title,
                Duration = TimeSpan.FromSeconds(30),
                VoterEligibility = VoteManager.VoterEligibility.All,
            };

            for (var i = 1; i <= options.Count; i++)
            {
                voteOptions.Options.Add((options[i-1], i));
            }

            voteOptions.SetInitiatorOrServer(initiator);

            if (initiator != null)
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{initiator} initiated a custom vote: {voteOptions.Title} - {string.Join("; ", voteOptions.Options.Select(x => x.text))}");
            else
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Initiated a custom vote: {voteOptions.Title} - {string.Join("; ", voteOptions.Options.Select(x => x.text))}");

            var vote = _voteManager.CreateVote(voteOptions);

            var webhookState = _voteWebhooks.CreateWebhookIfConfigured(voteOptions, _cfg.GetCVar(CCVars.DiscordVoteWebhook));

            vote.OnFinished += (_, eventArgs) =>
            {
                if (eventArgs.Winner == null)
                {
                    var ties = string.Join(", ", eventArgs.Winners.Select(c => options[(int) c]));
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {voteOptions.Title} finished as tie: {ties}");

                    if (showResultsInChat)
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-tie", ("title", voteOptions.Title), ("ties", ties)));
                }
                else
                {
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {voteOptions.Title} finished: {options[(int) eventArgs.Winner]}");

                    if (showResultsInChat)
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-win", ("title", voteOptions.Title), ("winner", options[(int) eventArgs.Winner])));
                }

                _voteWebhooks.UpdateWebhookIfConfigured(webhookState, eventArgs);
            };

            vote.OnCancelled += _ =>
            {
                _voteWebhooks.UpdateCancelledWebhookIfConfigured(webhookState);
            };
        }

        [CommandImplementation("startall")]
        public void StartAll(IInvocationContext ctx, string title, params string[] options)
        {
            StartCustomVote(ctx.Session, true, title, options.ToList());
        }
    }

    [AnyCommand]
    public sealed partial class VoteCommand : LocalizedEntityCommands
    {
        [Dependency] private IVoteManager _voteManager = default!;

        public override string Command => "vote";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player == null)
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-must-be-player"));
                return;
            }

            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 2), ("currentAmount", args.Length)));
                return;
            }

            if (!int.TryParse(args[0], out var voteId))
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-vote-id"));
                return;
            }

            if (!int.TryParse(args[1], out var voteOption))
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-vote-options"));
                return;
            }

            if (!_voteManager.TryGetVote(voteId, out var vote))
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-vote"));
                return;
            }

            int? optionN;
            if (voteOption == -1)
            {
                optionN = null;
            }
            else if (vote.IsValidOption(voteOption))
            {
                optionN = voteOption;
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-vote-on-execute-error-invalid-option"));
                return;
            }

            vote.CastVote(shell.Player!, optionN);
        }
    }

    [AnyCommand]
    public sealed partial class ListVotesCommand : LocalizedEntityCommands
    {
        [Dependency] private IVoteManager _voteManager = default!;

        public override string Command => "listvotes";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            foreach (var vote in _voteManager.ActiveVotes)
            {
                shell.WriteLine($"[{vote.Id}] {vote.InitiatorText}: {vote.Title}");
            }
        }
    }

    [AdminCommand(AdminFlags.Round)]
    public sealed partial class CancelVoteCommand : LocalizedEntityCommands
    {
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private IVoteManager _voteManager = default!;

        public override string Command => "cancelvote";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError(Loc.GetString("cmd-cancelvote-error-missing-vote-id"));
                return;
            }

            if (!int.TryParse(args[0], out var id) || !_voteManager.TryGetVote(id, out var vote))
            {
                shell.WriteError(Loc.GetString("cmd-cancelvote-error-invalid-vote-id"));
                return;
            }

            if (shell.Player != null)
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{shell.Player} canceled vote: {vote.Title}");
            else
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Canceled vote: {vote.Title}");
            vote.Cancel();
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = _voteManager.ActiveVotes
                    .OrderBy(v => v.Id)
                    .Select(v => new CompletionOption(v.Id.ToString(), v.Title));

                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-cancelvote-arg-id"));
            }

            return CompletionResult.Empty;
        }
    }
}
