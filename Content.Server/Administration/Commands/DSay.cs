using Content.Server.Interfaces.Chat;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class DSay : IConsoleCommand
    {
        public string Command => "dsay";

        public string Description => Loc.GetString("Sends a message to deadchat as an admin");

        public string Help => Loc.GetString($"Usage: {Command} <message>");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("Only players can use this command");
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
