using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class LOOCCommand : IConsoleCommand
    {
        [Dependency] private readonly IChatManager _chat = default!;

        public string Command => "looc";
        public string Description => "Send Local Out Of Character chat messages.";
        public string Help => "looc <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            _chat.RequestChat(player, message, ChatSelectChannel.LOOC);
        }
    }
}
