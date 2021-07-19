using Content.Server.Administration;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
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
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

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

                var gridId = new GridId(i);

                if (!mapManager.TryGetGrid(gridId, out _))
                {
                    shell.WriteError($"Grid \"{i}\" doesn't exist.");
                    continue;
                }

                foreach (var tile in atmosphereSystem.GetAllTileMixtures(gridId, true))
                {
                    tile.Clear();
                    atmosphereSystem.Merge(tile, mixture);
                    tile.Temperature = mixture.Temperature;
                }
            }
        }
    }
}
