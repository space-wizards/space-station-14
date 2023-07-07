using Content.Server._FTL.FTLPoints;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

[AdminCommand(AdminFlags.Mapping)]
public sealed class GeneratePointCommand : IConsoleCommand
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Command => "genpoint";
    public string Description => Loc.GetString("Generates an FTL point of a specific prototype, or a random weighted prototype if left unspecified.");
    public string Help => Loc.GetString("genpoint <point prototype>");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_prototypeManager.TryIndex<FTLPointPrototype>(args[1], out var prototype))
        {
            _entityManager.System<FTLPointsSystem>().GenerateDisposablePoint(prototype);
        }
        else
        {
            _entityManager.System<FTLPointsSystem>().GenerateDisposablePoint();
        }


        shell.WriteLine("Generated FTL point.");
    }
}
