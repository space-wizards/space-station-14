using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Shared.Console;
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
           if(!EntityUid.TryParse(gid, out var euid))
           {
               shell.WriteError($"Failed to parse euid '{gid}'.");
               return;
           }

           if (!TryComp(euid, out IMapGridComponent? gridComp))
           {
               shell.WriteError($"Euid '{euid}' does not exist or is not a grid.");
               return;
           }

           if (!TryComp(euid, out GridAtmosphereComponent? gridAtmosphere))
           {
               shell.WriteError($"Grid \"{euid}\" has no atmosphere component, try addatmos.");
               continue;
           }

           foreach (var (indices, tileMain) in gridAtmosphere.Tiles)
           {
               var tile = tileMain.Air;
               if (tile == null)
                   continue;

               tile.Clear();
               var mixtureId = 0;
               foreach (var entUid in gridComp.Grid.GetAnchoredEntities(indices))
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
