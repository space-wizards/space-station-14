using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using System;

namespace Content.Server.Commands.Mobs
{
    [AdminCommand(AdminFlags.Debug)]
    public class RemoveOverlayCommand : IClientCommand
    {
        public string Command => "rmoverlay";
        public string Description => "Removes all overlays of a given type.";
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
                    if (Enum.TryParse(args[0], out OverlayType overlayType))
                        overlayEffectsComponent.RemoveOverlaysOfType(overlayType);
                    else
                        shell.SendText(player, "Overlay type does not exist!");
                }
            }
        }
    }
}
