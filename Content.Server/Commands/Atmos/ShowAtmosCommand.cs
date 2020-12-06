#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.EntitySystems.Atmos;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class ShowAtmos : IClientCommand
    {
        public string Command => "showatmos";
        public string Description => "Toggles seeing atmos debug overlay.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "You must be a player to use this command.");
                return;
            }

            var atmosDebug = EntitySystem.Get<AtmosDebugOverlaySystem>();
            var enabled = atmosDebug.ToggleObserver(player);

            shell.SendText(player, enabled
                ? "Enabled the atmospherics debug overlay."
                : "Disabled the atmospherics debug overlay.");
        }
    }
}
