using Robust.Shared.Console;

namespace Content.Server.Atmos.Commands;

public sealed class AtmosSimulationPauseUnpause : LocalizedEntityCommands
{
    public override string Command => "atmospause";
    public override string Description => "Pauses or unpauses the atmosphere simulation for the provided grid entity.";
    public override string Help => $"Usage: {Command}";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {

    }
}
