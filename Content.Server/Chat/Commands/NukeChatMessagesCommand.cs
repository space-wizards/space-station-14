using Content.Server.Administration;
using Content.Server.Chat.V2.Repository;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    internal sealed class NukeChatMessagesUsernameCommand : IConsoleCommand
    {
        [Dependency] private readonly ChatRepository _repo = default!;

        public string Command => "nukeusernames";
        public string Description => "Delete all of the supplied usernames' chat messages posted during this round";
        public string Help => "nukeusernames <username> <username> <username> <username>...";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length <= 0)
            {
                shell.WriteError($"nuking messages failed: you forgot to input a username!");

                return;
            }

            foreach (var username in args[1..])
            {
                if (!_repo.NukeForUsername(username, out var reason))
                {
                    shell.WriteError($"nuke for username {args[0]} failed: {reason}");
                }
            }
        }

    [AdminCommand(AdminFlags.Admin)]
    internal sealed class NukeChatMessagesUserIdCommand : IConsoleCommand
    {
        [Dependency] private readonly ChatRepository _repo = default!;

        public string Command => "nukeuserids";
        public string Description => "Delete all of the supplied userIds' chat messages posted during this round";
        public string Help => "nukeuserids <username> <username> <username> <username>...";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length <= 0)
            {
                return;
            }

            foreach (var username in args[1..])
            {
                if (!_repo.NukeForUserId(username, out var reason))
                {
                    shell.WriteError($"nuke for userId {args[0]} failed: {reason}");
                }
            }
        }
    }
}

