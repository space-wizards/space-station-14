using Content.Server.Administration;
using Content.Server.Verbs;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server.Containers.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public class ToggleAllContextCommand : IConsoleCommand
    {
        public const string CommandName = "toggleallcontext";

        // ReSharper disable once StringLiteralTypo
        public string Command => CommandName;
        public string Description => "Toggles showing all entities visible on the context menu, even when they shouldn't be.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You need to be a player to use this command.");
                return;
            }

            EntitySystem.Get<VerbSystem>().ToggleSeeAllContext(player);
        }
    }
}
