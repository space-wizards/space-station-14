using Content.Shared.Speech;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Server.Bed.Sleep
{
    public abstract class SharedSleepingSystem : EntitySystem
    {
        [Dependency] private readonly BlindableSystem _blindableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SleepingComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<SleepingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<SleepingComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<SleepingComponent, CanSeeAttemptEvent>(OnSeeAttempt);
            SubscribeLocalEvent<SleepingComponent, EntityUnpausedEvent>(OnSleepUnpaused);
        }

        private void OnSleepUnpaused(EntityUid uid, SleepingComponent component, ref EntityUnpausedEvent args)
        {
            component.CoolDownEnd += args.PausedTime;
            Dirty(component);
        }

        private void OnStartup(EntityUid uid, SleepingComponent component, ComponentStartup args)
        {
            var ev = new SleepStateChangedEvent(true);
            RaiseLocalEvent(uid, ev);
            _blindableSystem.UpdateIsBlind(uid);
        }

        private void OnShutdown(EntityUid uid, SleepingComponent component, ComponentShutdown args)
        {
            var ev = new SleepStateChangedEvent(false);
            RaiseLocalEvent(uid, ev);
            _blindableSystem.UpdateIsBlind(uid);
        }

        private void OnSpeakAttempt(EntityUid uid, SleepingComponent component, SpeakAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnSeeAttempt(EntityUid uid, SleepingComponent component, CanSeeAttemptEvent args)
        {
            if (component.LifeStage <= ComponentLifeStage.Running)
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
