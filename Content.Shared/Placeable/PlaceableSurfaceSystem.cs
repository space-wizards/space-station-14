using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared.Placeable
{
    public class PlaceableSurfaceSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlaceableSurfaceComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PlaceableSurfaceComponent, ComponentHandleState>(OnHandleState);
        }

        public void SetPlaceable(EntityUid uid, bool isPlaceable, PlaceableSurfaceComponent? surface = null)
        {
            if (!Resolve(uid, ref surface))
                return;

            surface.IsPlaceable = isPlaceable;
            surface.Dirty();
        }

        public void SetPlaceCentered(EntityUid uid, bool placeCentered, PlaceableSurfaceComponent? surface = null)
        {
            if (!Resolve(uid, ref surface))
                return;

            surface.PlaceCentered = placeCentered;
            surface.Dirty();
        }

        public void SetPositionOffset(EntityUid uid, Vector2 offset, PlaceableSurfaceComponent? surface = null)
        {
            if (!Resolve(uid, ref surface))
                return;

            surface.PositionOffset = offset;
            surface.Dirty();
        }

        private void OnInteractUsing(EntityUid uid, PlaceableSurfaceComponent surface, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!surface.IsPlaceable)
                return;

            if(!args.User.TryGetComponent<SharedHandsComponent>(out var handComponent))
                return;

            if(!handComponent.TryDropEntity(args.Used, surface.Owner.Transform.Coordinates))
                return;

            if (surface.PlaceCentered)
                args.Used.Transform.LocalPosition = args.Target.Transform.LocalPosition + surface.PositionOffset;
            else
                args.Used.Transform.Coordinates = args.ClickLocation;

            args.Handled = true;
        }

        private void OnHandleState(EntityUid uid, PlaceableSurfaceComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not PlaceableSurfaceComponentState state)
                return;

            component.IsPlaceable = state.IsPlaceable;
            component.PlaceCentered = state.PlaceCentered;
            component.PositionOffset = state.PositionOffset;
        }
    }
}
