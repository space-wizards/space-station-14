using Content.Shared.Stunnable;
using Content.Shared.MobState.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;

namespace Content.Server.Bed.Sleep
{
    public sealed class SleepingSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MobStateComponent, SleepStateChangedEvent>(OnSleepStateChanged);
            SubscribeLocalEvent<SleepingComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<MobStateComponent, SleepActionEvent>(OnSleepAction);
        }

        private void OnSleepStateChanged(EntityUid uid, MobStateComponent component, SleepStateChangedEvent args)
        {
            if (args.FellAsleep)
            {
                EnsureComp<StunnedComponent>(uid);
                EnsureComp<KnockedDownComponent>(uid);
                return;
            }

            RemComp<StunnedComponent>(uid);
            RemComp<KnockedDownComponent>(uid);
        }

        private void OnDamageChanged(EntityUid uid, SleepingComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased || args.DamageDelta == null)
                return;

            if (args.DamageDelta.Total >= 5)
                TryWaking(uid);
        }

        private void OnSleepAction(EntityUid uid, MobStateComponent component, SleepActionEvent args)
        {
            AddComp<SleepingComponent>(args.Performer);
            args.Handled = true;
        }
        public bool TryWaking(EntityUid uid)
        {
            if (HasComp<ForcedSleepingComponent>(uid))
                return false;
            RemComp<SleepingComponent>(uid);
            return true;
        }
    }
}
