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
public sealed class SubstepAtmosCommand : LocalizedEntityCommands
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override string Command => "substepatmos";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var grid = default(EntityUid);

        switch (args.Length)
        {
            case 0:
                if (!EntityManager.TryGetComponent<TransformComponent>(shell.Player?.AttachedEntity,
                        out var playerxform) ||
                    playerxform.GridUid == null)
                {
                    shell.WriteError(Loc.GetString("cmd-error-no-grid-provided-or-invalid-grid"));
                    return;
                }

                grid = playerxform.GridUid.Value;
                break;
            case 1:
                if (!EntityUid.TryParse(args[0], out var parsedGrid) || !EntityManager.EntityExists(parsedGrid))
                {
                    shell.WriteError(Loc.GetString("cmd-error-couldnt-parse-entity"));
                    return;
                }

                grid = parsedGrid;
                break;
        }

        // i'm straight piratesoftwaremaxxing
        if (!EntityManager.TryGetComponent<GridAtmosphereComponent>(grid, out var gridAtmos))
        {
            shell.WriteError(Loc.GetString("cmd-error-no-gridatmosphere"));
            return;
        }

        if (!EntityManager.TryGetComponent<GasTileOverlayComponent>(grid, out var gasTile))
        {
            shell.WriteError(Loc.GetString("cmd-error-no-gastileoverlay"));
            return;
        }

        if (!EntityManager.TryGetComponent<MapGridComponent>(grid, out var mapGrid))
        {
            shell.WriteError(Loc.GetString("cmd-error-no-mapgrid"));
            return;
        }

        var xform = EntityManager.GetComponent<TransformComponent>(grid);

        if (xform.MapUid == null || xform.MapID == MapId.Nullspace)
        {
            shell.WriteError(Loc.GetString("cmd-error-no-valid-map"));
            return;
        }

        var newEnt =
            new Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent>(grid,
                gridAtmos,
                gasTile,
                mapGrid,
                xform);

        if (gridAtmos.Simulated)
        {
            shell.WriteLine(Loc.GetString("cmd-substepatmos-info-implicitly-paused-simulation",
                ("grid", EntityManager.ToPrettyString(grid))));
        }

        _atmosphereSystem.SetAtmosphereSimulation(newEnt, false);
        _atmosphereSystem.RunProcessingFull(newEnt, xform.MapUid.Value, _atmosphereSystem.AtmosTickRate);

        shell.WriteLine(Loc.GetString("cmd-substepatmos-info-substepped-grid", ("grid", EntityManager.ToPrettyString(grid))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<GridAtmosphereComponent>(args[0], EntityManager),
                Loc.GetString("cmd-substepatmos-completion-grid-substep"));
        }

        return CompletionResult.Empty;
    }
}
