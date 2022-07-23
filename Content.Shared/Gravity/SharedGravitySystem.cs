using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Shared.Gravity
{
    public abstract class SharedGravitySystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;

        public bool IsWeightless(EntityUid uid, PhysicsComponent? body = null, TransformComponent? xform = null)
        {
            Resolve(uid, ref body, false);

            if ((body?.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0)
                return false;

            if (TryComp<MovementIgnoreGravityComponent>(uid, out var ignoreGravityComponent))
                return ignoreGravityComponent.Weightless;

            if (!Resolve(uid, ref xform)) return true;

            bool gravityEnabled = false;

            // If grid / map has gravity
            if ((TryComp<GravityComponent>(xform.GridUid, out var gravity) ||
                 TryComp(xform.MapUid, out gravity)) && gravity.Enabled)
            {
                gravityEnabled = gravity.Enabled;

                if (gravityEnabled) return false;
            }

            // On the map then always weightless (unless it has gravity comp obv).
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
                return true;

            // Something holding us down
            if (_inventory.TryGetSlotEntity(uid, "shoes", out var ent))
            {
                if (TryComp<MagbootsComponent>(ent, out var boots) && boots.On)
                    return false;
            }

            if (!gravityEnabled || !xform.Coordinates.IsValid(EntityManager)) return true;

            var tile = grid.GetTileRef(xform.Coordinates).Tile;
            return tile.IsEmpty;
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInitialize);
        }

        private void HandleGridInitialize(GridInitializeEvent ev)
        {
            EntityManager.EnsureComponent<GravityComponent>(ev.EntityUid);
        }
    }
}
