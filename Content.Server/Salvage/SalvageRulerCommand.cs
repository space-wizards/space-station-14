using Content.Server.Preferences.Managers;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.IoC;

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

        if (shell.Player is not IPlayerSession player)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
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
        foreach (var mapGrid in _maps.GetAllMapGrids(entityTransform.MapID))
        {
            var aabb = _entities.GetComponent<TransformComponent>(mapGrid.GridEntityId).WorldMatrix.TransformBox(mapGrid.LocalAABB);
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

