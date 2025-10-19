using System.Linq;
using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    private void InitializeCommands()
    {
        // Fix Grid Atmos command.
        _consoleHost.RegisterCommand("fixgridatmos",
            "Makes every tile on a grid have a roundstart gas mix.",
            "fixgridatmos <grid Ids>", FixGridAtmosCommand, FixGridAtmosCommandCompletions);
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

       var mixtures = new GasMixture[9];
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

       // 6: (Walk-In) Freezer
       mixtures[6].AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesFreezer);
       mixtures[6].AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesFreezer);
       mixtures[6].Temperature = Atmospherics.FreezerTemp; // Little colder than an actual freezer but gives a grace period to get e.g. themomachines set up, should keep warm for a few door openings

       // 7: Nitrogen (101kpa) for vox rooms
       mixtures[7].AdjustMoles(Gas.Nitrogen, Atmospherics.MolesCellStandard);

       // 8: Air (GM)
       mixtures[8].AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesGasMiner);
       mixtures[8].AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesGasMiner);

       foreach (var arg in args)
       {
           if (!NetEntity.TryParse(arg, out var netEntity) || !TryGetEntity(netEntity, out var euid))
           {
               shell.WriteError($"Failed to parse euid '{arg}'.");
               return;
           }

           if (!TryComp(euid, out MapGridComponent? gridComp))
           {
               shell.WriteError($"Euid '{euid}' does not exist or is not a grid.");
               return;
           }

           if (!TryComp(euid, out GridAtmosphereComponent? gridAtmosphere))
           {
               shell.WriteError($"Grid \"{euid}\" has no atmosphere component, try addatmos.");
               continue;
           }

           // Force Invalidate & update air on all tiles
           Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> grid =
               new(euid.Value, gridAtmosphere, Comp<GasTileOverlayComponent>(euid.Value), gridComp, Transform(euid.Value));

           RebuildGridTiles(grid);

           var query = GetEntityQuery<AtmosFixMarkerComponent>();
           foreach (var (indices, tile) in gridAtmosphere.Tiles.ToArray())
           {
               if (tile.Air is not {Immutable: false} air)
                   continue;

               air.Clear();
               var mixtureId = 0;
               var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(grid, grid, indices);
               while (enumerator.MoveNext(out var entUid))
               {
                   if (query.TryComp(entUid, out var marker))
                       mixtureId = marker.Mode;
               }

               var mixture = mixtures[mixtureId];
               Merge(air, mixture);
               air.Temperature = mixture.Temperature;
           }
       }
    }

    /// <summary>
    /// Clears & re-creates all references to <see cref="TileAtmosphere"/>s stored on a grid.
    /// </summary>
    private void RebuildGridTiles(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent)
    {
        foreach (var indices in ent.Comp1.Tiles.Keys)
        {
            InvalidateVisuals((ent, ent), indices);
        }

        var atmos = ent.Comp1;
        atmos.MapTiles.Clear();
        atmos.ActiveTiles.Clear();
        atmos.ExcitedGroups.Clear();
        atmos.HotspotTiles.Clear();
        atmos.SuperconductivityTiles.Clear();
        atmos.HighPressureDelta.Clear();
        atmos.CurrentRunTiles.Clear();
        atmos.CurrentRunExcitedGroups.Clear();
        atmos.InvalidatedCoords.Clear();
        atmos.CurrentRunInvalidatedTiles.Clear();
        atmos.PossiblyDisconnectedTiles.Clear();
        atmos.Tiles.Clear();

        var volume = GetVolumeForTiles(ent);
        TryComp(ent.Comp4.MapUid, out MapAtmosphereComponent? mapAtmos);

        var enumerator = _map.GetAllTilesEnumerator(ent, ent);
        while (enumerator.MoveNext(out var tileRef))
        {
            var tile = GetOrNewTile(ent, ent, tileRef.Value.GridIndices);
            UpdateTileData(ent, mapAtmos, tile);
            UpdateAdjacentTiles(ent, tile, activate: true);
            UpdateTileAir(ent, tile, volume);
        }
    }

    private CompletionResult FixGridAtmosCommandCompletions(IConsoleShell shell, string[] args)
    {
        MapId? playerMap = null;
        if (shell.Player is { AttachedEntity: { } playerEnt })
            playerMap = Transform(playerEnt).MapID;

        var options = new List<CompletionOption>();

        if (playerMap == null)
            return CompletionResult.FromOptions(options);

        foreach (var grid in _mapManager.GetAllGrids(playerMap.Value).OrderBy(o => o.Owner))
        {
            var uid = grid.Owner;
            if (!TryComp(uid, out TransformComponent? gridXform))
                continue;

            options.Add(new CompletionOption(uid.ToString(), $"{MetaData(uid).EntityName} - Map {gridXform.MapID}"));
        }

        return CompletionResult.FromOptions(options);
    }
}
