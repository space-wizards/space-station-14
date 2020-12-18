#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.Chat;
using Content.Server.Players;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Chat
{
    [AnyCommand]
    internal class SayCommand : IClientCommand
    {
        public string Command => "say";
        public string Description => "Send chat messages to the local channel or a specified radio channel.";
        public string Help => "say <text>";

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

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            var playerEntity = player.AttachedEntity;

            if (playerEntity == null)
            {
                shell.SendText(player, "You don't have an entity!");
                return;
            }

            if (playerEntity.HasComponent<GhostComponent>())
                chat.SendDeadChat(player, message);
            else
            {
                var mindComponent = player.ContentData()?.Mind;

                if (mindComponent == null)
                {
                    shell.SendText(player, "You don't have a mind!");
                    return;
                }

                chat.EntitySay(mindComponent.OwnedEntity, message);
            }

        }
    }
}
