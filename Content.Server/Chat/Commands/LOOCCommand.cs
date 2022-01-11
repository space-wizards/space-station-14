using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.IoC;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal class LOOCCommand : IConsoleCommand
    {
        public string Command => "looc";
        public string Description => "Send Local Out Of Character chat messages.";
        public string Help => "looc <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("This command cannot be run from the server.");
                return;
            }

            if (player.Status != SessionStatus.InGame || player.AttachedEntity == null)
                return;

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            var mindComponent = player.ContentData()?.Mind;

            if (mindComponent == null)
            {
                shell.WriteError("You don't have a mind!");
                return;
            }

            if (mindComponent.OwnedEntity == null)
            {
                shell.WriteError("You don't have an entity!");
                return;
            }

            chat.EntityLOOC(mindComponent.OwnedEntity.Value, message);
        }
    }
}
