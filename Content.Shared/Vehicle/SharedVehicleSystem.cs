using Content.Shared.Vehicle.Components;
using Content.Shared.Actions;
using Content.Shared.Item;
using Robust.Shared.Serialization;

/// <summary>
/// Stores the VehicleVisuals and shared event
/// Nothing for a system but these need to be put somewhere in
/// Content.Shared
/// </summary>
namespace Content.Shared.Vehicle
{
    public sealed class SharedVehicleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<InVehicleComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        }

        private void OnPickupAttempt(EntityUid uid, InVehicleComponent component, GettingPickedUpAttemptEvent args)
        {
            if (component.Vehicle == null || !component.Vehicle.HasRider)
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

    /// <summary>
    /// Raised on the rider when someone is buckled to a vehicle.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class BuckledToVehicleEvent : EntityEventArgs
    {
        public EntityUid Vehicle;

        public EntityUid Rider;

        /// <summary>
        /// Whether they were buckled or unbuckled
        /// </summary>
        public bool Buckling;
        public BuckledToVehicleEvent(EntityUid vehicle, EntityUid rider, bool buckling)
        {
            Vehicle = vehicle;
            Rider = rider;
            Buckling = buckling;
        }
    }
}
