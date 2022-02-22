using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Shared.Placeable
{
    public sealed class PlaceableSurfaceSystem : EntitySystem
    {
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

            if(!EntityManager.TryGetComponent<SharedHandsComponent?>(args.User, out var handComponent))
                return;

            if(!handComponent.TryDropEntity(args.Used, EntityManager.GetComponent<TransformComponent>(surface.Owner).Coordinates))
                return;

            if (surface.PlaceCentered)
                EntityManager.GetComponent<TransformComponent>(args.Used).LocalPosition = EntityManager.GetComponent<TransformComponent>(uid).LocalPosition + surface.PositionOffset;
            else
                EntityManager.GetComponent<TransformComponent>(args.Used).Coordinates = args.ClickLocation;

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
