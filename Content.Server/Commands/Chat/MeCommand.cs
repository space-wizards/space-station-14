#nullable enable
using Content.Server.Administration;
using Content.Server.Interfaces.Chat;
using Content.Server.Players;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Chat
{
    [AnyCommand]
    internal class MeCommand : IConsoleCommand
    {
        public string Command => "me";
        public string Description => "Perform an action.";
        public string Help => "me <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("This command cannot be run from the server.");
                return;
            }

            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            if (args.Length < 1)
                return;

            var action = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(action))
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            var mindComponent = player.ContentData()?.Mind;

            if (mindComponent == null)
            {
                shell.WriteLine("You don't have a mind!");
                return;
            }

            chat.EntityMe(mindComponent.OwnedEntity, action);
        }
    }
}
