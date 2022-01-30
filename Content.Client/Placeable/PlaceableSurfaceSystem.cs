using Content.Shared.Placeable;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Placeable;

public sealed class PlaceableSurfaceSystem : SharedPlaceableSurfaceSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlaceableSurfaceComponent, ComponentHandleState>(OnHandleState);
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
