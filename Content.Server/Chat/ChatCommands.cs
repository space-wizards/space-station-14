using Content.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.IoC;

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

            chat.EntitySay(player.AttachedEntity, message);
        }
    }

    internal class OOCCommand : IClientCommand
    {
        public string Command => "ooc";
        public string Description => "Send Out of Character chat messages.";
        public string Help => "ooc <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var chat = IoCManager.Resolve<IChatManager>();
            chat.SendOOC(player, string.Join(" ", args));
        }
    }
}
