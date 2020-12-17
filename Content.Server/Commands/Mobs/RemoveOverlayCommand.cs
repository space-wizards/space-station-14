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
        public string Command => "rmoverlays";
        public string Description => "Removes all overlays of a given type.";
        public string Help => "rmoverlays <id>";

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
                    if(!overlayEffectsComponent.RemoveOverlaysOfType(args[0]))
                        shell.SendText(player, "Invalid OverlayType.");
                }
            }
        }
    }
}
