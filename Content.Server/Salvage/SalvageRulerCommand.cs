using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Salvage;

[AdminCommand(AdminFlags.Admin)]
public sealed class SalvageRulerCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IMapManager _maps = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    public override string Command => "salvageruler";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!CommandChecks.MustBeAttachedToEntity(shell, out _, out var entity) ||
            !CommandChecks.NeedExactlyZeroArguments(shell, args))
            return;

        var entityTransform = EntityManager.GetComponent<TransformComponent>(entity.Value);
        var total = Box2.UnitCentered;
        var first = true;
        foreach (var mapGrid in _maps.GetAllGrids(entityTransform.MapID))
        {
            var aabb = _xformSystem.GetWorldMatrix(mapGrid).TransformBox(mapGrid.Comp.LocalAABB);
            if (first)
            {
                total = aabb;
            }
            else
            {
                total = total.ExtendToContain(aabb.TopLeft);
                total = total.ExtendToContain(aabb.TopRight);
                total = total.ExtendToContain(aabb.BottomLeft);
                total = total.ExtendToContain(aabb.BottomRight);
            }
            first = false;
        }
        shell.WriteLine(total.ToString());
    }
}

