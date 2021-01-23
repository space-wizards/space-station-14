#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Administration;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public class HideContainedContextCommand : IServerCommand
    {
        public string Command => "hidecontainedcontext";
        public string Description => $"Reverts the effects of {ShowContainedContextCommand.CommandName}";
        public string Help => $"{Command}";

        public void Execute(IServerConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.WriteLine("You need to be a player to use this command.");
                return;
            }

            EntitySystem.Get<VerbSystem>().RemoveContainerVisibility(player);
        }
    }
}
