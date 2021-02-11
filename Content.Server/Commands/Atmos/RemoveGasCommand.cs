#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class RemoveGasCommand : IConsoleCommand
    {
        public string Command => "removegas";
        public string Description => "Removes an amount of gases.";
        public string Help => "removegas <X> <Y> <GridId> <amount> <ratio>\nIf <ratio> is true, amount will be treated as the ratio of gas to be removed.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 5) return;
            if(!int.TryParse(args[0], out var x)
               || !int.TryParse(args[1], out var y)
               || !int.TryParse(args[2], out var id)
               || !float.TryParse(args[3], out var amount)
               || !bool.TryParse(args[4], out var ratio)) return;

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
                shell.WriteLine("Can't remove gas from that tile.");
                return;
            }

            if (ratio)
                tile.Air.RemoveRatio(amount);
            else
                tile.Air.Remove(amount);

            tile.Invalidate();
        }
    }

}
