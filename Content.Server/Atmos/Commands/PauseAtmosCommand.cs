using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Atmos.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class PauseAtmosCommand : LocalizedEntityCommands
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override string Command => "pauseatmos";

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

        if (!EntityManager.TryGetComponent<GridAtmosphereComponent>(grid, out var gridAtmos))
        {
            shell.WriteError(Loc.GetString("cmd-error-no-gridatmosphere"));
            return;
        }

        var newEnt = new Entity<GridAtmosphereComponent>(grid, gridAtmos);

        _atmosphereSystem.SetAtmosphereSimulation(newEnt, !newEnt.Comp.Simulated);
        shell.WriteLine(Loc.GetString("cmd-pauseatmos-set-atmos-simulation",
            ("grid", EntityManager.ToPrettyString(grid)),
            ("state", newEnt.Comp.Simulated)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<GridAtmosphereComponent>(args[0], EntityManager),
                Loc.GetString("cmd-pauseatmos-completion-grid-pause"));
        }

        return CompletionResult.Empty;
    }
}
