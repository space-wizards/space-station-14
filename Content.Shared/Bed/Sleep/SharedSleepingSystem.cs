using Content.Shared.Eye.Blinding;
using Content.Shared.Speech;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;

namespace Content.Server.Bed.Sleep
{
    public sealed class SharedSleepingSystem : EntitySystem
    {
        [Dependency] private readonly SharedBlindingSystem _blindingSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SleepingComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SleepingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<SleepingComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        }

        private void OnInit(EntityUid uid, SleepingComponent component, ComponentInit args)
        {
            var ev = new SleepStateChangedEvent(true);
            RaiseLocalEvent(uid, ev, false);
            _blindingSystem.AdjustBlindSources(uid, true);
        }

        private void OnShutdown(EntityUid uid, SleepingComponent component, ComponentShutdown args)
        {
            var ev = new SleepStateChangedEvent(false);
            RaiseLocalEvent(uid, ev, false);
            _blindingSystem.AdjustBlindSources(uid, false);
        }

        private void OnSpeakAttempt(EntityUid uid, SleepingComponent component, SpeakAttemptEvent args)
        {
            args.Cancel();
        }
    }
}


public sealed class SleepActionEvent : InstantActionEvent {}

public sealed class WakeActionEvent : InstantActionEvent {}

/// <summary>
/// Raised on an entity when they fall asleep or wake up.
/// </summary>
public sealed class SleepStateChangedEvent : EntityEventArgs
{
    public bool FellAsleep = false;

    public SleepStateChangedEvent(bool fellAsleep)
    {
        FellAsleep = fellAsleep;
    }
}
