using Content.Server.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;

#nullable enable

namespace Content.Server.Voting
{
    [AnyCommand]
    public sealed class CreateVoteCommand : IConsoleCommand
    {
        public string Command => "createvote";
        public string Description => "Creates a vote";
        public string Help => "createvote <'restart'>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError("Need exactly one argument!");
                return;
            }

            var type = args[0];

            var mgr = IoCManager.Resolve<IVoteManager>();

            switch (type)
            {
                case "restart":
                    mgr.CreateRestartVote((IPlayerSession?) shell.Player);
                    break;
                default:
                    shell.WriteError("Invalid vote type!");
                    break;
            }
        }
    }

    [AnyCommand]
    public sealed class VoteCommand : IConsoleCommand
    {
        public string Command => "vote";
        public string Description => "Votes on an active vote";
        public string Help => "vote <voteId> <option>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player == null)
            {
                shell.WriteError("Must be a player");
                return;
            }

            if (args.Length != 2)
            {
                shell.WriteError("Expected two arguments.");
                return;
            }

            if (!int.TryParse(args[0], out var voteId))
            {
                shell.WriteError("Invalid vote ID");
                return;
            }

            if (!int.TryParse(args[1], out var voteOption))
            {
                shell.WriteError("Invalid vote options");
                return;
            }

            var mgr = IoCManager.Resolve<IVoteManager>();
            if (!mgr.TryGetVote(voteId, out var vote))
            {
                shell.WriteError("Invalid vote");
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
                shell.WriteError("Invalid option");
                return;
            }

            vote.CastVote((IPlayerSession) shell.Player!, optionN);
        }
    }
}
