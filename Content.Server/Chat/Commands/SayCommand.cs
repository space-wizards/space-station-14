using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Shared.Console;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class SayCommand : IConsoleCommand
    {
        [Dependency] private readonly IChatManager _chat = default!;

        public string Command => "say";
        public string Description => "Send chat messages to the local channel or a specified radio channel.";
        public string Help => "say <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (player.AttachedEntity is not {})
            {
                shell.WriteError("You don't have an entity!");
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            _chat.RequestChat(player, message, ChatSelectChannel.Local);
        }
    }
}
