using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    private void InitializeCommands()
    {
        // Fix Grid Atmos command.
        _consoleHost.RegisterCommand("fixgridatmos",
            "Makes every tile on a grid have a roundstart gas mix.",
            "fixgridatmos <grid Ids>", FixGridAtmosCommand);
    }

    private void ShutdownCommands()
    {
        _consoleHost.UnregisterCommand("fixgridatmos");
    }

    [AdminCommand(AdminFlags.Debug)]
    private void FixGridAtmosCommand(IConsoleShell shell, string argstr, string[] args)
    {
       if (args.Length == 0)
       {
           shell.WriteError("Not enough arguments.");
           return;
       }

       var mixtures = new GasMixture[6];
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

       // 5: Instant Plasmafire (r)
       mixtures[5].AdjustMoles(Gas.Oxygen, Atmospherics.MolesCellGasMiner);
       mixtures[5].AdjustMoles(Gas.Plasma, Atmospherics.MolesCellGasMiner);
       mixtures[5].Temperature = 5000f;

       foreach (var gid in args)
       {
           // I like offering detailed error messages, that's why I don't use one of the extension methods.
           if (!int.TryParse(gid, out var i) || i <= 0)
           {
               shell.WriteError($"Invalid grid ID \"{gid}\".");
               continue;
           }

           var gridId = new GridId(i);

           if (!_mapManager.TryGetGrid(gridId, out var mapGrid))
           {
               shell.WriteError($"Grid \"{i}\" doesn't exist.");
               continue;
           }

           if (!TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
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
                   if (!TryComp(entUid, out AtmosFixMarkerComponent? afm))
                       continue;
                   mixtureId = afm.Mode;
                   break;
               }
               var mixture = mixtures[mixtureId];
               Merge(tile, mixture);
               tile.Temperature = mixture.Temperature;

               InvalidateTile(gridAtmosphere, indices);
           }
       }
    }
}
