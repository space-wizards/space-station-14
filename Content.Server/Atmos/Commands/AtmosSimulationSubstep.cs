using Robust.Shared.Console;

namespace Content.Server.Atmos.Commands;

public sealed class AtmosSimulationSubstep : LocalizedEntityCommands
{
    public override string Command => "atmossubstep";
    public override string Description => "Substeps the atmosphere simulation by a single atmostick for the provided grid entity. " +
                                          "Implicitly pauses atmospherics simulation.";
    public override string Help => $"Usage: {Command} <GridUid>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not 1)
        {
            shell.WriteError("Invalid number of arguments.");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var grid) || !EntityManager.EntityExists(grid))
        {
            shell.WriteError("Entity provided could not be parsed or does not exist.");
        }

        // blah blah blah

    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            if (shell.Player is null ||
                !EntityManager.TryGetComponent<TransformComponent>(shell.Player.AttachedEntity, out var xform))
            {
                return CompletionResult.Empty;
            }

            if (xform.GridUid is { } grid)
            {
                return CompletionResult.FromHint(grid.Id.ToString());
            }
        }

        return CompletionResult.Empty;
    }
}
