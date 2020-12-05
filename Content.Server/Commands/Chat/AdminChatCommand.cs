#nullable enable
using Content.Server.Administration;
using Content.Server.Interfaces.Chat;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Chat
{
    [AdminCommand(AdminFlags.Admin)]
    internal class AdminChatCommand : IClientCommand
    {
        public string Command => "asay";
        public string Description => "Send chat messages to the private admin chat channel.";
        public string Help => "asay <text>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            chat.SendAdminChat(player, message);
        }
    }
}
