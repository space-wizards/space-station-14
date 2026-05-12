using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Salvage;

[AdminCommand(AdminFlags.Admin)]
sealed class SalvageRulerCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IMapManager _maps = default!;

    public string Command => "salvageruler";

    public string Description => Loc.GetString("salvage-ruler-command-description");

    public string Help => Loc.GetString("salvage-ruler-command-help-text", ("command",Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var entity = player.AttachedEntity;

        if (entity == null)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        var entityTransform = _entities.GetComponent<TransformComponent>(entity.Value);
        var total = Box2.UnitCentered;
        var first = true;
        foreach (var mapGrid in _maps.GetAllGrids(entityTransform.MapID))
        {
            var aabb = _entities.System<SharedTransformSystem>().GetWorldMatrix(mapGrid).TransformBox(mapGrid.Comp.LocalAABB);
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

