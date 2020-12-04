using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;

namespace Content.Server.Commands.Mobs
{
    [AdminCommand(AdminFlags.Debug)]
    public class RemoveOverlayCommand : IClientCommand
    {
        public string Command => "rmoverlay";
        public string Description => "Removes an overlay by its ID";
        public string Help => "rmoverlay <id>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Expected 1 argument.");
                return;
            }

            if (player?.AttachedEntity != null)
            {
                if (player.AttachedEntity.TryGetComponent(out ServerOverlayEffectsComponent overlayEffectsComponent))
                {
                    overlayEffectsComponent.RemoveOverlay(args[0]);
                }
            }
        }
    }
}
