using Content.Shared.Vehicle.Components;
using Content.Shared.Actions;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics.Pull;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle;

/// <summary>
/// Stores the VehicleVisuals and shared event
/// Nothing for a system but these need to be put somewhere in
/// Content.Shared
/// </summary>
public abstract partial class SharedVehicleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InVehicleComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<RiderComponent, PullAttemptEvent>(OnRiderPull);
        SubscribeLocalEvent<VehicleComponent, RefreshMovementSpeedModifiersEvent>(OnVehicleModifier);
    }

    private void OnVehicleModifier(EntityUid uid, VehicleComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.HasKey)
        {
            args.ModifySpeed(0f, 0f);
        }
    }

    private void OnPickupAttempt(EntityUid uid, InVehicleComponent component, GettingPickedUpAttemptEvent args)
    {
        if (!component.Vehicle.HasRider)
            return;

        if (component.Vehicle.Rider != args.User)
            args.Cancel();
    }
}

/// <summary>
/// Stores the vehicle's draw depth mostly
/// </summary>
[Serializable, NetSerializable]
public enum VehicleVisuals : byte
{
    /// <summary>
    /// What layer the vehicle should draw on (assumed integer)
    /// </summary>
    DrawDepth,
    /// <summary>
    /// Whether the wheels should be turning
    /// </summary>
    AutoAnimate
}
/// <summary>
/// Raised when someone honks a vehicle horn
/// </summary>
public sealed class HonkActionEvent : InstantActionEvent { }

