using Content.Shared.Clothing;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Shared.Movement.EntitySystems;

public abstract class SharedWeightlessSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public bool IsWeightless(EntityUid uid, PhysicsComponent? body = null, TransformComponent? xform = null)
    {
        Resolve(uid, ref body, false);

        if ((body?.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0 ||
            HasComp<MovementIgnoreGravityComponent>(uid)) return false;

        Resolve(uid, ref xform, false);

        if (xform == null) return true;

        var gridId = xform.GridID;

        if (!_mapManager.TryGetGrid(gridId, out var grid))
        {
            // Not on a grid = no gravity for now.
            // In the future, may want to allow maps to override to always have gravity instead.
            return true;
        }

        if (_inventory.TryGetSlotEntity(uid, "shoes", out var ent))
        {
            if (TryComp<SharedMagbootsComponent>(ent, out var boots) && boots.On)
                return false;
        }

        if (!Comp<GravityComponent>(grid.GridEntityId).Enabled)
        {
            return true;
        }

        var coordinates = xform.Coordinates;

        if (!coordinates.IsValid(EntityManager))
            return true;

        var tile = grid.GetTileRef(coordinates).Tile;
        return tile.IsEmpty;
    }
}
