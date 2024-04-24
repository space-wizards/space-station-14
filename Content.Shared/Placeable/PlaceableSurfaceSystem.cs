using System.Numerics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Storage.Components;

namespace Content.Shared.Placeable
{
    public sealed class PlaceableSurfaceSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlaceableSurfaceComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        }

        public void SetPlaceable(EntityUid uid, bool isPlaceable, PlaceableSurfaceComponent? surface = null)
        {
            if (!Resolve(uid, ref surface, false))
                return;

            surface.IsPlaceable = isPlaceable;
            Dirty(uid, surface);
        }

        public void SetPlaceCentered(EntityUid uid, bool placeCentered, PlaceableSurfaceComponent? surface = null)
        {
            if (!Resolve(uid, ref surface))
                return;

            surface.PlaceCentered = placeCentered;
            Dirty(uid, surface);
        }

        public void SetPositionOffset(EntityUid uid, Vector2 offset, PlaceableSurfaceComponent? surface = null)
        {
            if (!Resolve(uid, ref surface))
                return;

            surface.PositionOffset = offset;
            Dirty(uid, surface);
        }

        private void OnAfterInteractUsing(EntityUid uid, PlaceableSurfaceComponent surface, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (!surface.IsPlaceable)
                return;

            // 99% of the time they want to dump the stuff inside on the table, they can manually place with q if they really need to.
            // Just causes prediction CBT otherwise.
            if (HasComp<DumpableComponent>(args.Used))
                return;

            if (!_handsSystem.TryDrop(args.User, args.Used))
                return;

            if (surface.PlaceCentered)
                Transform(args.Used).LocalPosition = Transform(uid).LocalPosition + surface.PositionOffset;
            else
                Transform(args.Used).Coordinates = args.ClickLocation;

            args.Handled = true;
        }
    }
}
