using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Damage
{
    public sealed class SlowOnDamageSystem : EntitySystem
    {
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SlowOnDamageComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<SlowOnDamageComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        }

        private void OnRefreshMovespeed(EntityUid uid, SlowOnDamageComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            if (!EntityManager.TryGetComponent<DamageableComponent>(uid, out var damage))
                return;

            if (damage.TotalDamage == FixedPoint2.Zero)
                return;

            // Get closest threshold
            FixedPoint2 closest = FixedPoint2.Zero;
            var total = damage.TotalDamage;
            foreach (var thres in component.SpeedModifierThresholds)
            {
                if (total >= thres.Key && thres.Key > closest)
                    closest = thres.Key;
            }

            if (closest != FixedPoint2.Zero)
            {
                var speed = component.SpeedModifierThresholds[closest];
                args.ModifySpeed(speed, speed);
            }
        }

        private void OnDamageChanged(EntityUid uid, SlowOnDamageComponent component, DamageChangedEvent args)
        {
            // We -could- only refresh if it crossed a threshold but that would kind of be a lot of duplicated
            // code and this isn't a super hot path anyway since basically only humans have this

            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
        }
    }
}
