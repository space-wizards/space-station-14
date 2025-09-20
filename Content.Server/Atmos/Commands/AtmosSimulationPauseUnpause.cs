using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Atmos.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class AtmosSimulationPauseUnpause : LocalizedEntityCommands
{
    public override string Command => "pauseatmos";
    public override string Description => Loc.GetString("atmos-pause-description");
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

        if (!EntityManager.TryGetComponent<GridAtmosphereComponent>(grid, out var gridAtmos))
        {
            shell.WriteError(Loc.GetString("error-no-gridatmosphere"));
            return;
        }

        var newEnt = new Entity<GridAtmosphereComponent>(grid, gridAtmos);

        var atmosSys = EntityManager.System<AtmosphereSystem>();

        atmosSys.SetAtmosphereSimulation(newEnt, !newEnt.Comp.Simulated);
        shell.WriteLine(Loc.GetString("set-atmos-simulation") + " " + EntityManager.ToPrettyString(grid) +
                        Loc.GetString("to-state") + " " + newEnt.Comp.Simulated);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("completion-grid-pause"));
        }

        return CompletionResult.Empty;
    }
}
