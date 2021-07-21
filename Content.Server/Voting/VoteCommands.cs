using System;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Voting.Managers;
using Content.Shared.Administration;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Voting
{
    [AnyCommand]
    public sealed class CreateVoteCommand : IConsoleCommand
    {
        public string Command => "createvote";
        public string Description => Loc.GetString("create-vote-command-description");
        public string Help => Loc.GetString("create-vote-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-need-exactly-one-argument"));
                return;
            }

            if (!Enum.TryParse<StandardVoteType>(args[0], ignoreCase: true, out var type))
            {
                shell.WriteError(Loc.GetString("create-vote-command-invalid-vote-type"));
                return;
            }

            var mgr = IoCManager.Resolve<IVoteManager>();

            if (shell.Player != null && !mgr.CanCallVote((IPlayerSession) shell.Player, type))
            {
                shell.WriteError(Loc.GetString("create-vote-command-cannot-call-vote-now"));
                return;
            }

            mgr.CreateStandardVote((IPlayerSession?) shell.Player, type);
        }
    }

    [AdminCommand(AdminFlags.Fun)]
    public sealed class CreateCustomCommand : IConsoleCommand
    {
        public string Command => "customvote";
        public string Description => Loc.GetString("create-custom-command-description");
        public string Help => Loc.GetString("create-custom-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 10)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments",("lower", 3), ("upper", 10)));
                return;
            }

            var title = args[0];

            var mgr = IoCManager.Resolve<IVoteManager>();

            var options = new VoteOptions
            {
                Title = title,
                Duration = TimeSpan.FromSeconds(30),
            };

            for (var i = 1; i < args.Length; i++)
            {
                options.Options.Add((args[i], i));
            }

            options.SetInitiatorOrServer((IPlayerSession?) shell.Player);

            var vote = mgr.CreateVote(options);

            vote.OnFinished += (_, eventArgs) =>
            {
                var chatMgr = IoCManager.Resolve<IChatManager>();
                if (eventArgs.Winner == null)
                {
                    var ties = string.Join(", ", eventArgs.Winners.Select(c => args[(int) c]));
                    chatMgr.DispatchServerAnnouncement(Loc.GetString("create-custom-command-on-finished-tie",("ties", ties)));
                }
                else
                {
                    chatMgr.DispatchServerAnnouncement(Loc.GetString("create-custom-command-on-finished-win",("winner", args[(int) eventArgs.Winner])));
                }
            };
        }
    }


    [AnyCommand]
    public sealed class VoteCommand : IConsoleCommand
    {
        public string Command => "vote";
        public string Description => Loc.GetString("vote-command-description");
        public string Help => Loc.GetString("vote-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player == null)
            {
                shell.WriteError(Loc.GetString("vote-command-on-execute-error-must-be-player"));
                return;
            }

            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 2), ("currentAmount", args.Length)));
                return;
            }

            if (!int.TryParse(args[0], out var voteId))
            {
                shell.WriteError(Loc.GetString("vote-command-on-execute-error-invalid-vote-id"));
                return;
            }

            if (!int.TryParse(args[1], out var voteOption))
            {
                shell.WriteError(Loc.GetString("vote-command-on-execute-error-invalid-vote-options"));
                return;
            }

            var mgr = IoCManager.Resolve<IVoteManager>();
            if (!mgr.TryGetVote(voteId, out var vote))
            {
                shell.WriteError(Loc.GetString("vote-command-on-execute-error-invalid-vote"));
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
                shell.WriteError(Loc.GetString("vote-command-on-execute-error-invalid-option"));
                return;
            }

            vote.CastVote((IPlayerSession) shell.Player!, optionN);
        }
    }

    [AnyCommand]
    public sealed class ListVotesCommand : IConsoleCommand
    {
        public string Command => "listvotes";
        public string Description => Loc.GetString("list-votes-command-description");
        public string Help => Loc.GetString("list-votes-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IVoteManager>();

            foreach (var vote in mgr.ActiveVotes)
            {
                shell.WriteLine($"[{vote.Id}] {vote.InitiatorText}: {vote.Title}");
            }
        }
    }

    [AdminCommand(AdminFlags.Admin)]
    public sealed class CancelVoteCommand : IConsoleCommand
    {
        public string Command => "cancelvote";
        public string Description => Loc.GetString("cancel-vote-command-description");
        public string Help => Loc.GetString("cancel-vote-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var mgr = IoCManager.Resolve<IVoteManager>();

            if (args.Length < 1)
            {
                shell.WriteError(Loc.GetString("cancel-vote-command-on-execute-error-missing-vote-id"));
                return;
            }

            if (!int.TryParse(args[0], out var id) || !mgr.TryGetVote(id, out var vote))
            {
                shell.WriteError(Loc.GetString("cancel-vote-command-on-execute-error-invalid-vote-id"));
                return;
            }

            vote.Cancel();
        }
    }
}
