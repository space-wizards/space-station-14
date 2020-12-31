#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class AddGasCommand : IClientCommand
    {
        public string Command => "addgas";
        public string Description => "Adds gas at a certain position.";
        public string Help => "addgas <X> <Y> <GridId> <Gas> <moles>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length < 5) return;
            if(!int.TryParse(args[0], out var x)
               || !int.TryParse(args[1], out var y)
               || !int.TryParse(args[2], out var id)
               || !(AtmosCommandUtils.TryParseGasID(args[3], out var gasId))
               || !float.TryParse(args[4], out var moles)) return;

            var gridId = new GridId(id);

            var mapMan = IoCManager.Resolve<IMapManager>();

            if (!gridId.IsValid() || !mapMan.TryGetGrid(gridId, out var gridComp))
            {
                shell.SendText(player, "Invalid grid ID.");
                return;
            }

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetEntity(gridComp.GridEntityId, out var grid))
            {
                shell.SendText(player, "Failed to get grid entity.");
                return;
            }

            if (!grid.HasComponent<GridAtmosphereComponent>())
            {
                shell.SendText(player, "Grid doesn't have an atmosphere.");
                return;
            }

            var gam = grid.GetComponent<GridAtmosphereComponent>();
            var indices = new Vector2i(x, y);
            var tile = gam.GetTile(indices);

            if (tile == null)
            {
                shell.SendText(player, "Invalid coordinates.");
                return;
            }

            if (tile.Air == null)
            {
                shell.SendText(player, "Can't add gas to that tile.");
                return;
            }

            tile.Air.AdjustMoles(gasId, moles);
            tile.Invalidate();
        }
    }
}
