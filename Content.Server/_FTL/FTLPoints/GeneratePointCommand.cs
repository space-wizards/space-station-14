using Content.Server._FTL.FTLPoints;
using Robust.Shared.Console;

sealed class GeneratePointCommand : LocalizedCommands
{
    [Dependency] private readonly EntityManager _ent = default!;

    public override string Command => "genpoint";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _ent.System<FTLPointsSystem>().GenerateDisposablePoint();

        shell.WriteLine("Generated FTL point.");
    }
}
