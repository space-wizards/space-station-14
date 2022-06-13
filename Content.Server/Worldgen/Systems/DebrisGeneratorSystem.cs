using System.Linq;
using Content.Server.Administration;
using Content.Server.Worldgen.Components;
using Content.Server.Worldgen.Floorplanners;
using Content.Server.Worldgen.Prototypes;
using Content.Server.Worldgen.Systems.Planes;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Worldgen.Systems;

public sealed class DebrisGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly FloorplanningPlaneSystem _floorplanningPlaneSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        _consoleHost.RegisterCommand("generatedebris", "Generates the given debris", "generatedebris <prototype>", GenerateDebrisCommand);
    }

    [AdminCommand(AdminFlags.Debug)]
    private void GenerateDebrisCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (!_prototypeManager.TryIndex<DebrisPrototype>(args[0], out var proto))
            return;

        var xform = Transform(shell.Player!.AttachedEntity!.Value);

        TryGenerateDebris(xform.MapPosition, proto, out _);
    }

    public bool TryGenerateDebris(MapCoordinates coordinates, DebrisPrototype prototype, out EntityUid? debris)
    {
        var grid = _mapManager.CreateGrid(coordinates.MapId);
        grid.WorldPosition = coordinates.Position;
        var floorplanners = prototype.Floorplanners;

        var success = true;
        foreach (var floorplanner in floorplanners)
        {
            success &= _floorplanningPlaneSystem.ConstructTiling(floorplanner, grid.GridEntityId, Vector2.Zero, null);
        }

        debris = grid.GridEntityId;

        return success;
    }
}
