using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Vehicle;

public sealed class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private EyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RiderComponent, ComponentStartup>(OnRiderStartup);
        SubscribeLocalEvent<RiderComponent, ComponentShutdown>(OnRiderShutdown);
        SubscribeLocalEvent<RiderComponent, ComponentHandleState>(OnRiderHandleState);
        SubscribeLocalEvent<VehicleComponent, AppearanceChangeEvent>(OnVehicleAppearanceChange);
    }

    private void OnRiderStartup(EntityUid uid, RiderComponent component, ComponentStartup args)
    {
        // Center the player's eye on the vehicle
        if (TryComp(uid, out EyeComponent? eyeComp))
        {
            _eye.SetTarget(uid, eyeComp.Target ?? component.Vehicle, eyeComp);
        }
    }

    private void OnRiderShutdown(EntityUid uid, RiderComponent component, ComponentShutdown args)
    {
        // reset the riders eye centering.
        if (TryComp(uid, out EyeComponent? eyeComp))
        {
            _eye.SetTarget(uid, null, eyeComp);
        }
    }

    private void OnRiderHandleState(EntityUid uid, RiderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RiderComponentState state)
            return;

        var entity = EnsureEntity<RiderComponent>(state.Entity, uid);

        if (TryComp(uid, out EyeComponent? eyeComp) && eyeComp.Target == component.Vehicle)
        {
            _eye.SetTarget(uid, entity, eyeComp);
        }

        component.Vehicle = entity;
    }

    private void OnVehicleAppearanceChange(EntityUid uid, VehicleComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (component.HideRider
            && Appearance.TryGetData<bool>(uid, VehicleVisuals.HideRider, out var hide, args.Component)
            && TryComp<SpriteComponent>(component.LastRider, out var riderSprite))
            riderSprite.Visible = !hide;

        // First check is for the sprite itself
        if (Appearance.TryGetData<int>(uid, VehicleVisuals.DrawDepth, out var drawDepth, args.Component))
            args.Sprite.DrawDepth = drawDepth;

        // Set vehicle layer to animated or not (i.e. are the wheels turning or not)
        if (component.AutoAnimate
            && Appearance.TryGetData<bool>(uid, VehicleVisuals.AutoAnimate, out var autoAnimate, args.Component))
            args.Sprite.LayerSetAutoAnimated(VehicleVisualLayers.AutoAnimate, autoAnimate);
    }
}

public enum VehicleVisualLayers : byte
{
    /// Layer for the vehicle's wheels
    AutoAnimate,
}
