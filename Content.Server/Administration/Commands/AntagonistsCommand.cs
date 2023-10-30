using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AntagonistsCommand : IConsoleCommand
    {
        public string Command => "antagonists";
        public string Description => "Opens the list of antagonists.";
        public string Help => "Usage: antagonists";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("This does not work from the server console.");
                return;
            }
        }
    }
}
