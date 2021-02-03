#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public class ShowContainedContextCommand : IConsoleCommand
    {
        public const string CommandName = "showcontainedcontext";

        // ReSharper disable once StringLiteralTypo
        public string Command => CommandName;
        public string Description => "Makes contained entities visible on the context menu, even when they shouldn't be.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You need to be a player to use this command.");
                return;
            }

            EntitySystem.Get<VerbSystem>().AddContainerVisibility(player);
        }
    }
}
