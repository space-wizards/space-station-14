using Content.Shared.Eye.Blinding;
using Content.Shared.Stunnable;
using Content.Shared.Speech;
using Content.Shared.Damage;
using Content.Shared.Actions;
using Content.Shared.MobState.Components;

namespace Content.Shared.Bed.Sleep
{
    public sealed class SleepingSystem : EntitySystem
    {
        [Dependency] private readonly SharedBlindingSystem _blindingSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedSleepingComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SharedSleepingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<SharedSleepingComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        }

        private void OnInit(EntityUid uid, SharedSleepingComponent component, ComponentInit args)
        {
            _blindingSystem.AdjustBlindSources(uid, true);
        }

        private void OnShutdown(EntityUid uid, SharedSleepingComponent component, ComponentShutdown args)
        {
            _blindingSystem.AdjustBlindSources(uid, false);
        }
        private void OnSpeakAttempt(EntityUid uid, SharedSleepingComponent component, SpeakAttemptEvent args)
        {
            args.Cancel();
        }
    }
}

public sealed class SleepActionEvent : InstantActionEvent { }
