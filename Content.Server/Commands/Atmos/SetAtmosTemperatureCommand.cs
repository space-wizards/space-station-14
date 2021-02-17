#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class SetAtmosTemperatureCommand : IConsoleCommand
    {
        public string Command => "setatmostemp";
        public string Description => "Sets a grid's temperature (in kelvin).";
        public string Help => "Usage: setatmostemp <GridId> <Temperature>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2) return;
            if(!int.TryParse(args[0], out var id)
               || !float.TryParse(args[1], out var temperature)) return;

            var gridId = new GridId(id);

            var mapMan = IoCManager.Resolve<IMapManager>();

            if (temperature < Atmospherics.TCMB)
            {
                shell.WriteLine("Invalid temperature.");
                return;
            }

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

            var tiles = 0;
            foreach (var tile in gam)
            {
                if (tile.Air == null)
                    continue;

                tiles++;

                tile.Air.Temperature = temperature;
                tile.Invalidate();
            }

            shell.WriteLine($"Changed the temperature of {tiles} tiles.");
        }
    }
}
