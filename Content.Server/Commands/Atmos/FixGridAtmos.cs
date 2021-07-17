using Content.Server.Administration;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class FixGridAtmos : IConsoleCommand
    {
        public string Command => "fixgridatmos";
        public string Description => "Makes every tile on a grid have a roundstart gas mix.";
        public string Help => $"{Command} <grid Ids>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0)
            {
                shell.WriteError("Not enough arguments.");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var mixture = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };
            mixture.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
            mixture.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);

            foreach (var gid in args)
            {
                // I like offering detailed error messages, that's why I don't use one of the extension methods.
                if (!int.TryParse(gid, out var i) || i <= 0)
                {
                    shell.WriteError($"Invalid grid ID \"{gid}\".");
                    continue;
                }

                if (!mapManager.TryGetGrid(new GridId(i), out var grid))
                {
                    shell.WriteError($"Grid \"{i}\" doesn't exist.");
                    continue;
                }

                if (!entityManager.TryGetEntity(grid.GridEntityId, out var entity))
                {
                    shell.WriteError($"Grid entity for grid \"{i}\" doesn't exist.");
                    continue;
                }

                var gridAtmosphere = new GridAtmosphereComponent() {Owner = entity};

                // Inject dependencies manually or a NRE will eat your face.
                IoCManager.InjectDependencies(gridAtmosphere);

                entityManager.ComponentManager.AddComponent(entity, gridAtmosphere, true);

                gridAtmosphere.RepopulateTiles();

                foreach (var tile in gridAtmosphere)
                {
                    tile.Air = (GasMixture) mixture.Clone();
                    tile.Air.Volume = gridAtmosphere.GetVolumeForCells(1);
                    tile.Invalidate();
                }
            }
        }
    }
}
