using Content.Shared.Storage.Components;
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
            SubscribeLocalEvent<PlaceableSurfaceComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<PlaceableSurfaceComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnGetState(EntityUid uid, PlaceableSurfaceComponent component, ref ComponentGetState args)
        {
            args.State = new PlaceableSurfaceComponentState(component.IsPlaceable, component.PlaceCentered, component.PositionOffset);
        }

        public void SetPlaceable(EntityUid uid, bool isPlaceable, PlaceableSurfaceComponent? surface = null)
        {
            if (!Resolve(uid, ref surface))
                return;

            surface.IsPlaceable = isPlaceable;
            Dirty(surface);
        }

        public void SetPlaceCentered(EntityUid uid, bool placeCentered, PlaceableSurfaceComponent? surface = null)
        {
            if (!Resolve(uid, ref surface))
                return;

            surface.PlaceCentered = placeCentered;
            Dirty(surface);
        }

        public void SetPositionOffset(EntityUid uid, Vector2 offset, PlaceableSurfaceComponent? surface = null)
        {
            if (!Resolve(uid, ref surface))
                return;

            surface.PositionOffset = offset;
            Dirty(surface);
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
