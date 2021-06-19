#nullable enable
using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class AddGasCommand : IConsoleCommand
    {
        public string Command => "addgas";
        public string Description => "Adds gas at a certain position.";
        public string Help => "addgas <X> <Y> <GridId> <Gas> <moles>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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
                shell.WriteLine("Invalid grid ID.");
                return;
            }

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetEntity(gridComp.GridEntityId, out var grid))
            {
                shell.WriteLine("Failed to get grid entity.");
                return;
            }

            if (!grid.HasComponent<GridAtmosphereComponent>())
            {
                shell.WriteLine("Grid doesn't have an atmosphere.");
                return;
            }

            var gam = grid.GetComponent<GridAtmosphereComponent>();
            var indices = new Vector2i(x, y);
            var tile = gam.GetTile(indices);

            if (tile == null)
            {
                shell.WriteLine("Invalid coordinates.");
                return;
            }

            if (tile.Air == null)
            {
                shell.WriteLine("Can't add gas to that tile.");
                return;
            }

            tile.Air.AdjustMoles(gasId, moles);
            tile.Invalidate();
        }
    }
}
