using Content.Shared.Eye.Blinding;
using Content.Shared.Speech;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;

namespace Content.Server.Bed.Sleep
{
    public abstract class SharedSleepingSystem : EntitySystem
    {
        [Dependency] private readonly SharedBlindingSystem _blindingSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SleepingComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SleepingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<SleepingComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<SleepingComponent, EntityUnpausedEvent>(OnSleepUnpaused);
        }

        private void OnSleepUnpaused(EntityUid uid, SleepingComponent component, ref EntityUnpausedEvent args)
        {
            component.CoolDownEnd += args.PausedTime;
            Dirty(component);
        }

        private void OnInit(EntityUid uid, SleepingComponent component, ComponentInit args)
        {
            var ev = new SleepStateChangedEvent(true);
            RaiseLocalEvent(uid, ev);
            _blindingSystem.AdjustBlindSources(uid, 1);
        }

        private void OnShutdown(EntityUid uid, SleepingComponent component, ComponentShutdown args)
        {
            var ev = new SleepStateChangedEvent(false);
            RaiseLocalEvent(uid, ev);
            _blindingSystem.AdjustBlindSources(uid, -1);
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
