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
using Robust.Server;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server.Voting
{
    [AnyCommand]
    public sealed class CreateVoteCommand : IConsoleCommand
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public string Command => "createvote";
        public string Description => Loc.GetString("cmd-createvote-desc");
        public string Help => Loc.GetString("cmd-createvote-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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

            var mgr = IoCManager.Resolve<IVoteManager>();

            if (shell.Player != null && !mgr.CanCallVote(shell.Player, type))
            {
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{shell.Player} failed to start {type.ToString()} vote");
                shell.WriteError(Loc.GetString("cmd-createvote-cannot-call-vote-now"));
                return;
            }

            mgr.CreateStandardVote(shell.Player, type, args.Skip(1).ToArray());
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = Enum.GetNames<StandardVoteType>();
                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-createvote-arg-vote-type"));
            }

            return CompletionResult.Empty;
        }
    }

    [AdminCommand(AdminFlags.Moderator)]
    public sealed class CreateCustomCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly VoteWebhooks _voteWebhooks = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private ISawmill _sawmill = default!;

        private const int MaxArgCount = 10;

        public override string Command => "customvote";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _sawmill = Logger.GetSawmill("vote");

            if (args.Length < 3 || args.Length > MaxArgCount)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 3), ("upper", 10)));
                return;
            }

            var title = args[0];

            var options = new VoteOptions
            {
                Title = title,
                Duration = TimeSpan.FromSeconds(30),
            };

            for (var i = 1; i < args.Length; i++)
            {
                options.Options.Add((args[i], i));
            }

            options.SetInitiatorOrServer(shell.Player);

            if (shell.Player != null)
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"{shell.Player} initiated a custom vote: {options.Title} - {string.Join("; ", options.Options.Select(x => x.text))}");
            else
                _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Initiated a custom vote: {options.Title} - {string.Join("; ", options.Options.Select(x => x.text))}");

            var vote = _voteManager.CreateVote(options);

            var webhookState = _voteWebhooks.CreateWebhookIfConfigured(options, _cfg.GetCVar(CCVars.DiscordVoteWebhook));

            vote.OnFinished += (_, eventArgs) =>
            {
                if (eventArgs.Winner == null)
                {
                    var ties = string.Join(", ", eventArgs.Winners.Select(c => args[(int) c]));
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {options.Title} finished as tie: {ties}");
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-tie", ("ties", ties)));
                }
                else
                {
                    _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Custom vote {options.Title} finished: {args[(int) eventArgs.Winner]}");
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("cmd-customvote-on-finished-win", ("winner", args[(int) eventArgs.Winner])));
                }

                _voteWebhooks.UpdateWebhookIfConfigured(webhookState, eventArgs);
            };

            vote.OnCancelled += _ =>
            {
                _voteWebhooks.UpdateCancelledWebhookIfConfigured(webhookState);
            };
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
                return CompletionResult.FromHint(Loc.GetString("cmd-customvote-arg-title"));

            if (args.Length > MaxArgCount)
                return CompletionResult.Empty;

            var n = args.Length - 1;
            return CompletionResult.FromHint(Loc.GetString("cmd-customvote-arg-option-n", ("n", n)));
        }
    }

    [AnyCommand]
    public sealed class VoteCommand : IConsoleCommand
    {
        public string Command => "vote";
        public string Description => Loc.GetString("cmd-vote-desc");
        public string Help => Loc.GetString("cmd-vote-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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

            var mgr = IoCManager.Resolve<IVoteManager>();
            if (!mgr.TryGetVote(voteId, out var vote))
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
    public sealed class ListVotesCommand : IConsoleCommand
    {
        public string Command => "listvotes";
        public string Description => Loc.GetString("cmd-listvotes-desc");
        public string Help => Loc.GetString("cmd-listvotes-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IVoteManager>();

            foreach (var vote in mgr.ActiveVotes)
            {
                shell.WriteLine($"[{vote.Id}] {vote.InitiatorText}: {vote.Title}");
            }
        }
    }

    [AdminCommand(AdminFlags.Moderator)]
    public sealed class CancelVoteCommand : IConsoleCommand
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public string Command => "cancelvote";
        public string Description => Loc.GetString("cmd-cancelvote-desc");
        public string Help => Loc.GetString("cmd-cancelvote-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IVoteManager>();

            if (args.Length < 1)
            {
                shell.WriteError(Loc.GetString("cmd-cancelvote-error-missing-vote-id"));
                return;
            }

            if (!int.TryParse(args[0], out var id) || !mgr.TryGetVote(id, out var vote))
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

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            var mgr = IoCManager.Resolve<IVoteManager>();
            if (args.Length == 1)
            {
                var options = mgr.ActiveVotes
                    .OrderBy(v => v.Id)
                    .Select(v => new CompletionOption(v.Id.ToString(), v.Title));

                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-cancelvote-arg-id"));
            }

            return CompletionResult.Empty;
        }
    }
}
