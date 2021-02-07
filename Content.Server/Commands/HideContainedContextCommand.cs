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
    public class HideContainedContextCommand : IConsoleCommand
    {
        public string Command => "hidecontainedcontext";
        public string Description => $"Reverts the effects of {ShowContainedContextCommand.CommandName}";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You need to be a player to use this command.");
                return;
            }

            EntitySystem.Get<VerbSystem>().RemoveContainerVisibility(player);
        }
    }
}
