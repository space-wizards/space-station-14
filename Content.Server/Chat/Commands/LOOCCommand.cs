using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class LOOCCommand : IConsoleCommand
    {
        public string Command => "looc";
        public string Description => "Send Local Out Of Character chat messages.";
        public string Help => "looc <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError("This command cannot be run from the server.");
                return;
            }

            if (player.AttachedEntity is not { Valid: true } entity)
                return;

            if (player.Status != SessionStatus.InGame)
                return;

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            EntitySystem.Get<ChatSystem>().TrySendInGameOOCMessage(entity, message, InGameOOCChatType.Looc, false, shell, player);
        }
    }
}
