using Content.Shared.Eye.Blinding;
using Content.Shared.Stunnable;
using Content.Shared.Speech;
using Content.Shared.Damage;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.MobState.Components;

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
            SubscribeLocalEvent<SleepingComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<MobStateComponent, SleepActionEvent>(OnSleepAction);
        }

        private void OnInit(EntityUid uid, SleepingComponent component, ComponentInit args)
        {
            AddComp<StunnedComponent>(uid);
            _blindingSystem.AdjustBlindSources(uid, true);
        }

        private void OnShutdown(EntityUid uid, SleepingComponent component, ComponentShutdown args)
        {
            RemComp<StunnedComponent>(uid);
            _blindingSystem.AdjustBlindSources(uid, false);
        }

        private void OnDamageChanged(EntityUid uid, SleepingComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased || args.DamageDelta == null)
                return;

            if (args.DamageDelta.Total >= 5)
                TryWaking(uid);
        }

        private void OnSpeakAttempt(EntityUid uid, SleepingComponent component, SpeakAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnSleepAction(EntityUid uid, MobStateComponent component, SleepActionEvent args)
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

public sealed class SleepActionEvent : InstantActionEvent {}
