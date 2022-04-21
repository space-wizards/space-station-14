using Content.Server.Buckle.Components;
using Content.Shared.Vehicle.Components;
using Content.Shared.MobState;
using Content.Shared.Stunnable;
using Content.Shared.Hands;

namespace Content.Server.Vehicle
{
    public sealed class RiderSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RiderComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<RiderComponent, GotParalyzedEvent>(OnParalyzed);
            SubscribeLocalEvent<RiderComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        /// <summary>
        /// Kick the rider off the vehicle if they press q / drop the virtual item
        /// </summary>
        private void OnVirtualItemDeleted(EntityUid uid, RiderComponent component, VirtualItemDeletedEvent args)
        {
            if (args.BlockingEntity == component.Vehicle?.Owner)
            {
                if (!TryComp<BuckleComponent>(uid, out var buckle))
                    return;

                buckle.TryUnbuckle(uid, true);
            }
        }

        /// <summary>
        /// Kick the rider off the vehicle if they get stunned
        /// </summary>
        private void OnParalyzed(EntityUid uid, RiderComponent rider, GotParalyzedEvent args)
        {
            if (!TryComp<BuckleComponent>(uid, out var buckle))
                return;

            buckle.TryUnbuckle(uid, true);
        }

        /// <summary>
        /// Kick the rider off the vehicle if they go into crit or die.
        /// </summary>
        private void OnMobStateChanged(EntityUid uid, RiderComponent rider, MobStateChangedEvent args)
        {
            if (!TryComp<BuckleComponent>(uid, out var buckle))
                return;

            if (args.Component.IsCritical() || args.Component.IsDead())
            {
                buckle.TryUnbuckle(uid, true);
            }
        }
    }
}
