using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos.Components;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class AtmosSimulationSubstep : LocalizedEntityCommands
{
    public override string Command => "substepatmos";
    public override string Description => Loc.GetString("atmos-substep-description");
    public override string Help => $"Usage: {Command} <GridUid>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var grid = default(EntityUid);

        switch (args.Length)
        {
            case 0:
            {
                if (shell.Player is null ||
                    !EntityManager.TryGetComponent<TransformComponent>(shell.Player.AttachedEntity, out var playerxform) ||
                    playerxform.GridUid == null)
                {
                    shell.WriteError(Loc.GetString("error-no-grid-provided-or-invalid-grid"));
                    return;
                }

                grid = playerxform.GridUid.Value;
                break;
            }
            case 1:
            {
                if (!EntityUid.TryParse(args[0], out var parsedGrid) || !EntityManager.EntityExists(parsedGrid))
                {
                    shell.WriteError(Loc.GetString("error-couldnt-parse-entity"));
                    return;
                }

                grid = parsedGrid;
                break;
            }
        }

        // i'm straight piratesoftwaremaxxing
        if (!EntityManager.TryGetComponent<GridAtmosphereComponent>(grid, out var gridAtmos))
        {
            shell.WriteError(Loc.GetString("error-no-gridatmosphere"));
            return;
        }

        if (!EntityManager.TryGetComponent<GasTileOverlayComponent>(grid, out var gasTile))
        {
            shell.WriteError(Loc.GetString("error-no-gastileoverlay"));
            return;
        }

        if (!EntityManager.TryGetComponent<MapGridComponent>(grid, out var mapGrid))
        {
            shell.WriteError(Loc.GetString("error-no-mapgrid"));
            return;
        }

        if (!EntityManager.TryGetComponent<TransformComponent>(grid, out var xform))
        {
            shell.WriteError(Loc.GetString("error-no-xform"));
            return;
        }

        if (xform.MapUid == null || xform.MapID == MapId.Nullspace)
        {
            shell.WriteError(Loc.GetString("error-no-valid-map"));
            return;
        }

        var newEnt =
            new Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>(grid,
                gridAtmos,
                gasTile,
                mapGrid,
                xform);

        var atmosSys = EntityManager.System<AtmosphereSystem>();

        if (gridAtmos.Simulated)
        {
            shell.WriteLine(Loc.GetString("info-implicitly-paused-simulation") + " " + EntityManager.ToPrettyString(grid));
        }

        atmosSys.SetAtmosphereSimulation(newEnt, false);
        atmosSys.RunProcessingFull(newEnt, xform.MapUid.Value, atmosSys.AtmosTickRate);

        shell.WriteLine(Loc.GetString("info-substepped-grid") + " " + EntityManager.ToPrettyString(grid));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("completion-grid-substep"));
        }

        return CompletionResult.Empty;
    }
}
