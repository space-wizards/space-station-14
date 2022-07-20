using Content.Server.Buckle.Components;
using Content.Shared.Vehicle.Components;
using Content.Shared.MobState;
using Content.Server.Standing;
using Content.Shared.Hands;
using Robust.Shared.GameStates;

namespace Content.Server.Vehicle
{
    public sealed partial class VehicleSystem
    {
        private void InitializeRider()
        {
            SubscribeLocalEvent<RiderComponent, ComponentStartup>(OnRiderStartup);
            SubscribeLocalEvent<RiderComponent, ComponentShutdown>(OnRiderShutdown);
            SubscribeLocalEvent<RiderComponent, MetaFlagRemoveAttemptEvent>(OnRiderRemoval);
            SubscribeLocalEvent<RiderComponent, ComponentGetState>(OnRiderGetState);
            SubscribeLocalEvent<RiderComponent, ComponentGetStateAttemptEvent>(OnRiderGetStateAttempt);
            SubscribeLocalEvent<RiderComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<RiderComponent, FellDownEvent>(OnFallDown);
            SubscribeLocalEvent<RiderComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnRiderRemoval(EntityUid uid, RiderComponent component, ref MetaFlagRemoveAttemptEvent args)
        {
            if ((args.ToRemove & MetaDataFlags.EntitySpecific) != 0x0)
                args.ToRemove = MetaDataFlags.None;
        }

        private void OnRiderStartup(EntityUid uid, RiderComponent component, ComponentStartup args)
        {
            _metadata.AddFlag(uid, MetaDataFlags.EntitySpecific);
        }

        private void OnRiderShutdown(EntityUid uid, RiderComponent component, ComponentShutdown args)
        {
            _metadata.RemoveFlag(uid, MetaDataFlags.EntitySpecific);
        }

        private void OnRiderGetStateAttempt(EntityUid uid, RiderComponent component, ref ComponentGetStateAttemptEvent args)
        {
            if (uid != args.Player.AttachedEntity)
                args.Cancelled = true;
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
