using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.Chat;
using Content.Server.Observer;
using Content.Server.Players;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Server.Chat
{
    internal class SayCommand : IClientCommand
    {
        public string Command => "say";
        public string Description => "Send chat messages to the local channel or a specified radio channel.";
        public string Help => "say <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            if (args.Length < 1)
                return;

            var chat = IoCManager.Resolve<IChatManager>();

            var message = string.Join(" ", args);


            if (player.AttachedEntity.HasComponent<GhostComponent>())
                chat.SendDeadChat(player, message);
            else
            {
                // This is a hacky way of getting the Players Mind
                var mindComponent = player.ContentData().Mind;
                // Say the message, as the Mind of the player.
                chat.EntitySay(mindComponent.OwnedEntity, message);
            }

        }
    }

    internal class MeCommand : IClientCommand
    {
        public string Command => "me";
        public string Description => "Perform an action.";
        public string Help => "me <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            if (args.Length < 1)
                return;

            var chat = IoCManager.Resolve<IChatManager>();

            var action = string.Join(" ", args);

            // As EntitySay, Hacky way of getting the Player's Mind
            var mindComponent = player.ContentData().Mind;
            // Now do the /me as the Player's Mind.
            chat.EntityMe(mindComponent.OwnedEntity, action);
        }
    }

    internal class OOCCommand : IClientCommand
    {
        public string Command => "ooc";
        public string Description => "Send Out Of Character chat messages.";
        public string Help => "ooc <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var chat = IoCManager.Resolve<IChatManager>();
            chat.SendOOC(player, string.Join(" ", args));
        }
    }
}
