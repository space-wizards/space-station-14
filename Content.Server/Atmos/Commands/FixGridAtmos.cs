using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Components;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Commands
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
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();

            var mixtures = new GasMixture[5];
            for (var i = 0; i < mixtures.Length; i++)
                mixtures[i] = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };

            // 0: Air
            mixtures[0].AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
            mixtures[0].AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);

            // 1: Vaccum

            // 2: Oxygen (GM)
            mixtures[2].AdjustMoles(Gas.Oxygen, Atmospherics.MolesCellGasMiner);

            // 3: Nitrogen (GM)
            mixtures[3].AdjustMoles(Gas.Nitrogen, Atmospherics.MolesCellGasMiner);

            // 4: Plasma (GM)
            mixtures[4].AdjustMoles(Gas.Plasma, Atmospherics.MolesCellGasMiner);

            foreach (var gid in args)
            {
                // I like offering detailed error messages, that's why I don't use one of the extension methods.
                if (!int.TryParse(gid, out var i) || i <= 0)
                {
                    shell.WriteError($"Invalid grid ID \"{gid}\".");
                    continue;
                }

                var gridId = new GridId(i);

                if (!mapManager.TryGetGrid(gridId, out var mapGrid))
                {
                    shell.WriteError($"Grid \"{i}\" doesn't exist.");
                    continue;
                }

                if (!entityManager.TryGetComponent(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
                {
                    shell.WriteError($"Grid \"{i}\" has no atmosphere component, try addatmos.");
                    continue;
                }

                foreach (var (indices, tileMain) in gridAtmosphere.Tiles)
                {
                    var tile = tileMain.Air;
                    if (tile == null)
                        continue;

                    tile.Clear();
                    var mixtureId = 0;
                    foreach (var entUid in mapGrid.GetAnchoredEntities(indices))
                    {
                        if (!entityManager.TryGetComponent(entUid, out AtmosFixMarkerComponent? afm))
                            continue;
                        mixtureId = afm.Mode;
                        break;
                    }
                    var mixture = mixtures[mixtureId];
                    atmosphereSystem.Merge(tile, mixture);
                    tile.Temperature = mixture.Temperature;

                    atmosphereSystem.InvalidateTile(gridAtmosphere, indices);
                }
            }
        }
    }
}
