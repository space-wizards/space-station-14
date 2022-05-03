using Content.Server.Buckle.Components;
using Content.Shared.Vehicle.Components;
using Content.Shared.MobState;
using Content.Server.Standing;
using Content.Shared.Hands;

namespace Content.Server.Vehicle
{
    public sealed class RiderSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RiderComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<RiderComponent, FellDownEvent>(OnFallDown);
            SubscribeLocalEvent<RiderComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        /// <summary>
        /// Kick the rider off the vehicle if they press q / drop the virtual item
        /// </summary>
        private void OnVirtualItemDeleted(EntityUid uid, RiderComponent component, VirtualItemDeletedEvent args)
        {
            if (args.BlockingEntity == component.Vehicle?.Owner)
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
            if (args.Component.IsCritical() || args.Component.IsDead())
            {
                UnbuckleFromVehicle(uid);
            }
        }

        public void UnbuckleFromVehicle(EntityUid uid)
        {
            if (!TryComp<BuckleComponent>(uid, out var buckle))
                return;

            buckle.TryUnbuckle(uid, true);
        }
    }
}
