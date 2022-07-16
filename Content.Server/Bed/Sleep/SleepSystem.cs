using Content.Shared.Eye.Blinding;
using Content.Shared.Stunnable;
using Content.Shared.Speech;
using Content.Shared.Damage;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;

namespace Content.Server.Bed.Sleep
{
    public sealed class SleepingSystem : EntitySystem
    {
        [Dependency] private readonly SharedBlindingSystem _blindingSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SleepingComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SleepingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<SleepingComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<SleepActionEvent>(OnSleepAction);
        }

        private void OnInit(EntityUid uid, SleepingComponent component, ComponentInit args)
        {
            AddComp<StunnedComponent>(uid);
        }

        private void OnShutdown(EntityUid uid, SleepingComponent component, ComponentShutdown args)
        {
            RemComp<StunnedComponent>(uid);
        }

        private void OnDamageChanged(EntityUid uid, SleepingComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased || args.DamageDelta == null)
                return;

            if (args.DamageDelta.Total >= 5)
                TryWaking(uid);
        }

        private void OnSleepAction(SleepActionEvent args)
        {
            Logger.Error("Received event...");
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
