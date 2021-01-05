using Content.Server.Interfaces.Chat;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class DSay : IClientCommand
    {
        public string Command => "dsay";

        public string Description => Loc.GetString("Sends a message to deadchat as an admin");

        public string Help => Loc.GetString($"Usage: {Command} <message>");

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                shell.SendText((IPlayerSession) null, "Only players can use this command");
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();

            chat.SendAdminDeadChat(player, message);
            
        }
    }
}
