using Content.Server.Standing;
using Content.Shared.Hands;
using Content.Shared.Mobs;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Vehicle
{
    public sealed partial class VehicleSystem
    {
        private void InitializeRider()
        {
            SubscribeLocalEvent<RiderComponent, ComponentGetState>(OnRiderGetState);
            SubscribeLocalEvent<RiderComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<RiderComponent, FellDownEvent>(OnFallDown);
            SubscribeLocalEvent<RiderComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnRiderGetState(EntityUid uid, RiderComponent component, ref ComponentGetState args)
        {
            args.State = new RiderComponentState()
            {
                Entity = component.Vehicle,
            };
        }

        /// <summary>
        /// Kick the rider off the vehicle if they press q / drop the virtual item
        /// </summary>
        private void OnVirtualItemDeleted(EntityUid uid, RiderComponent component, VirtualItemDeletedEvent args)
        {
            if (args.BlockingEntity == component.Vehicle)
            {
                UnbuckleFromVehicle(uid);
            }
        }

        /// <summary>
        /// Kick the rider off the vehicle if they get stunned
        /// </summary>
        private void OnFallDown(EntityUid uid, RiderComponent rider, FellDownEvent args)
        {
           UnbuckleFromVehicle(uid);
        }

        /// <summary>
        /// Kick the rider off the vehicle if they go into crit or die.
        /// </summary>
        private void OnMobStateChanged(EntityUid uid, RiderComponent rider, MobStateChangedEvent args)
        {
            if (args.NewMobState is MobState.Critical or MobState.Dead)
            {
                UnbuckleFromVehicle(uid);
            }
        }

        public void UnbuckleFromVehicle(EntityUid uid)
        {
            _buckle.TryUnbuckle(uid, uid, true);
        }
    }
}
