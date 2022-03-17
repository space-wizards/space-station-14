using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.GameStates;

namespace Content.Shared.Placeable
{
    public sealed class PlaceableSurfaceSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlaceableSurfaceComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
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

        private void OnAfterInteractUsing(EntityUid uid, PlaceableSurfaceComponent surface, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (!surface.IsPlaceable)
                return;

            if (!_handsSystem.TryDrop(args.User, args.Used))
                return;

            if (surface.PlaceCentered)
                Transform(args.Used).LocalPosition = Transform(uid).LocalPosition + surface.PositionOffset;
            else
                Transform(args.Used).Coordinates = args.ClickLocation;

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
