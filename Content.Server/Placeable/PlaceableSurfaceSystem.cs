using Content.Server.Construction;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Server.Placeable;

public sealed class PlaceableSurfaceSystem : SharedPlaceableSurfaceSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlaceableSurfaceComponent, InteractUsingEvent>(OnInteractUsing, after:new[] {typeof(ConstructionSystem)});
        SubscribeLocalEvent<PlaceableSurfaceComponent, ComponentGetState>(OnPlaceableGetState);
    }

    private void OnPlaceableGetState(EntityUid uid, PlaceableSurfaceComponent component, ref ComponentGetState args)
    {
        args.State = new PlaceableSurfaceComponentState(component.IsPlaceable, component.PlaceCentered, component.PositionOffset);
    }

    private void OnInteractUsing(EntityUid uid, PlaceableSurfaceComponent surface, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!surface.IsPlaceable)
            return;

        if(!EntityManager.TryGetComponent<SharedHandsComponent?>(args.User, out var handComponent))
            return;

        if (!args.ClickLocation.IsValid(EntityManager))
            return;

        if(!handComponent.TryDropEntity(args.Used, Transform(surface.Owner).Coordinates))
            return;

        if (surface.PlaceCentered)
            Transform(args.Used).LocalPosition = EntityManager.GetComponent<TransformComponent>(args.Target).LocalPosition + surface.PositionOffset;
        else
            Transform(args.Used).Coordinates = args.ClickLocation;

        args.Handled = true;
    }
}
