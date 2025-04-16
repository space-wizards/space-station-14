using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Engineering.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Engineering.EntitySystems
{
    public sealed class SpawnAfterInteractSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedMapSystem _maps = default!;
        [Dependency] private readonly SharedStackSystem _stackSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly TurfSystem _turfSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnAfterInteractComponent, AfterInteractEvent>(HandleAfterInteract);
            SubscribeLocalEvent<SpawnAfterInteractComponent, SpawnAfterInteractDoAfterEvent>(OnSpawnDoAfter);
        }

        private void OnSpawnDoAfter(Entity<SpawnAfterInteractComponent> ent, ref SpawnAfterInteractDoAfterEvent ev)
        {
            if (EntityManager.TryGetComponent(ent.Owner, out StackComponent? stackComp)
                && ent.Comp.RemoveOnInteract && !_stackSystem.Use(ent.Owner, 1, stackComp))
            {
                return;
            }

            var coords = GetCoordinates(ev.TargetLocation);

            if (!coords.IsValid(EntityManager) || !_transform.TrySnapToGrid(coords, out var snapped))
                return;

            PredictedSpawnAttachedTo(ent.Comp.Prototype, snapped);

            if (ent.Comp.RemoveOnInteract && stackComp == null)
                PredictedQueueDel(ent.Owner);
        }

        private void HandleAfterInteract(EntityUid uid, SpawnAfterInteractComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach && !component.IgnoreDistance)
                return;
            if (string.IsNullOrEmpty(component.Prototype))
                return;

            if (!_maps.TryGetTileRef(args.ClickLocation, out var tileRef))
                return;

            if (!IsTileClear(tileRef))
                return;

            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.DoAfterTime, new SpawnAfterInteractDoAfterEvent()
            {
                TargetLocation = GetNetCoordinates(args.ClickLocation)
            }, uid)
            {
                BreakOnMove = true,
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
        }

        private bool IsTileClear(TileRef tileRef)
        {
            return tileRef.Tile.IsEmpty == false && !_turfSystem.IsTileBlocked(tileRef, CollisionGroup.MobMask);
        }
    }

    [Serializable, NetSerializable]
    public sealed partial class SpawnAfterInteractDoAfterEvent : DoAfterEvent
    {
        public NetCoordinates TargetLocation;

        public override DoAfterEvent Clone()
        {
            return this;
        }
    }
}
