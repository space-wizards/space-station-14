using System;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Interfaces.Chat;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

#nullable enable

namespace Content.Server.Voting
{
    [AnyCommand]
    public sealed class CreateVoteCommand : IConsoleCommand
    {
        public string Command => "createvote";
        public string Description => "Creates a vote";
        public string Help => "Usage: createvote <'restart'|'preset'>";

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
                case "preset":
                    mgr.CreatePresetVote((IPlayerSession?) shell.Player);
                    break;
                default:
                    shell.WriteError("Invalid vote type!");
                    break;
            }
        }
    }

    [AdminCommand(AdminFlags.Fun)]
    public sealed class CreateCustomCommand : IConsoleCommand
    {
        public string Command => "customvote";
        public string Description => "Creates a custom vote";
        public string Help => "createvote <title> <option1> <option2> [option3...]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 10)
            {
                shell.WriteError("Need 3 to 10 arguments!");
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
                    chatMgr.DispatchServerAnnouncement(Loc.GetString("Tie between {0}!", ties));
                }
                else
                {
                    chatMgr.DispatchServerAnnouncement(Loc.GetString("{0} wins!", args[(int) eventArgs.Winner]));
                }
            };
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
