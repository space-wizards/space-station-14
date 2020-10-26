#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Commands
{
    public class HideContainedContextCommand : IClientCommand
    {
        public string Command => "hidecontainedcontext";
        public string Description => $"Reverts the effects of {ShowContainedContextCommand.CommandName}";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "You need to be a player to use this command.");
                return;
            }

            EntitySystem.Get<VerbSystem>().RemoveContainerVisibility(player);
        }
    }
}
