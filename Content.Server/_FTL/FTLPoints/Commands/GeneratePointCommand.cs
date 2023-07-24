using System.Linq;
using Content.Server._FTL.FTLPoints.Prototypes;
using Content.Server._FTL.FTLPoints.Systems;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._FTL.FTLPoints.Commands;

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
        if (args.Count() == 1 && _prototypeManager.TryIndex<FTLPointPrototype>(args[0], out var prototype))
        {
            _entityManager.System<FTLPointsSystem>().GenerateDisposablePoint(prototype);
            shell.WriteLine("Generated FTL point.");
        }
        else
        {
            _entityManager.System<FTLPointsSystem>().GenerateDisposablePoint();
            shell.WriteLine("Generated random FTL point.");
        }
    }
}