#nullable enable
using Content.Server.Administration;
using Content.Server.Interfaces.Chat;
using Content.Server.Players;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Chat
{
    [AnyCommand]
    internal class MeCommand : IClientCommand
    {
        public string Command => "me";
        public string Description => "Perform an action.";
        public string Help => "me <text>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "This command cannot be run from the server.");
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
                shell.SendText(player, "You don't have a mind!");
                return;
            }

            chat.EntityMe(mindComponent.OwnedEntity, action);
        }
    }
}
