using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeTimer()
    {
        SubscribeLocalEvent<RepeatingTriggerComponent, MapInitEvent>(OnRepeatInit);
        SubscribeLocalEvent<RepeatingTriggerComponent, ComponentHandleState>(OnRepeatHandleState);
        SubscribeLocalEvent<RepeatingTriggerComponent, EntityTimerEvent>(OnRepeatingTimer);
        SubscribeLocalEvent<RandomTimerTriggerComponent, MapInitEvent>(OnRandomInit);
        SubscribeLocalEvent<TimerTriggerComponent, ComponentShutdown>(OnTimerShutdown);
        SubscribeLocalEvent<TimerTriggerComponent, ComponentHandleState>(OnTimerHandleState);
        SubscribeLocalEvent<TimerTriggerComponent, ExaminedEvent>(OnTimerExamined);
        SubscribeLocalEvent<TimerTriggerComponent, TriggerEvent>(OnTimerTriggered);
        SubscribeLocalEvent<TimerTriggerComponent, GetVerbsEvent<AlternativeVerb>>(OnTimerGetAltVerbs);
        SubscribeLocalEvent<ActiveTimerTriggerComponent, ComponentInit>(OnActiveTimerInit);
        SubscribeLocalEvent<ActiveTimerTriggerComponent, EntityTimerEvent>(OnActiveTimer);
    }

    // set the time of the first trigger after being spawned
    private void OnRepeatInit(Entity<RepeatingTriggerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextTrigger = _timing.CurTime + ent.Comp.Delay;
        Dirty(ent);
        ScheduleRepeatingTimer(ent);
    }

    private void OnRepeatHandleState(Entity<RepeatingTriggerComponent> ent, ref ComponentHandleState args)
    {
        ScheduleRepeatingTimer(ent);
    }

    private void OnRandomInit(Entity<RandomTimerTriggerComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient) // Nextfloat will mispredict, so we set it on the server and dirty it
            return;

        if (!TryComp<TimerTriggerComponent>(ent, out var timerTriggerComp))
            return;

        timerTriggerComp.Delay = TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.Min, ent.Comp.Max));
        Dirty(ent.Owner, timerTriggerComp);
    }

    private void OnTimerShutdown(Entity<TimerTriggerComponent> ent, ref ComponentShutdown args)
    {
        RemComp<ActiveTimerTriggerComponent>(ent);
    }

    private void OnTimerHandleState(Entity<TimerTriggerComponent> ent, ref ComponentHandleState args)
    {
        ScheduleActiveTimer(ent.Owner, ent.Comp);
    }

    private void OnActiveTimerInit(Entity<ActiveTimerTriggerComponent> ent, ref ComponentInit args)
    {
        if (TryComp<TimerTriggerComponent>(ent, out var timer))
            ScheduleActiveTimer(ent, timer);
    }

    private void OnTimerExamined(Entity<TimerTriggerComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange && ent.Comp.Examinable)
            args.PushText(Loc.GetString("timer-trigger-examine", ("time", ent.Comp.Delay.TotalSeconds)));
    }

    private void OnTimerTriggered(Entity<TimerTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        args.Handled |= ActivateTimerTrigger(ent.AsNullable(), args.User);
    }

    /// <summary>
    /// Add an alt-click interaction that cycles through delays.
    /// </summary>
    private void OnTimerGetAltVerbs(Entity<TimerTriggerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        if (ent.Comp.DelayOptions.Count <= 1)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Category = TimerOptions,
            Text = Loc.GetString("timer-trigger-verb-cycle"),
            Act = () => CycleDelay(ent, user),
            Priority = 1
        });

        foreach (var option in ent.Comp.DelayOptions)
        {
            if (MathHelper.CloseTo(option.TotalSeconds, ent.Comp.Delay.TotalSeconds))
            {
                args.Verbs.Add(new AlternativeVerb
                {
                    Category = TimerOptions,
                    Text = Loc.GetString("timer-trigger-verb-set-current", ("time", option.TotalSeconds)),
                    Disabled = true,
                    Priority = -100 * (int)option.TotalSeconds
                });
            }
            else
            {
                args.Verbs.Add(new AlternativeVerb
                {
                    Category = TimerOptions,
                    Text = Loc.GetString("timer-trigger-verb-set", ("time", option.TotalSeconds)),
                    Priority = -100 * (int)option.TotalSeconds,
                    Act = () =>
                    {
                        ent.Comp.Delay = option;
                        Dirty(ent);
                        _popup.PopupEntity(Loc.GetString("timer-trigger-popup-set", ("time", option.TotalSeconds)), user, user);
                    }
                });
            }
        }
    }

    public static readonly VerbCategory TimerOptions = new("verb-categories-timer", "/Textures/Interface/VerbIcons/clock.svg.192dpi.png");

    /// <summary>
    /// Select the next entry from the DelayOptions.
    /// </summary>
    private void CycleDelay(Entity<TimerTriggerComponent> ent, EntityUid? user)
    {
        if (ent.Comp.DelayOptions.Count <= 1)
            return;

        // This is somewhat inefficient, but its good enough. This is run rarely, and the lists should be short.

        ent.Comp.DelayOptions.Sort();
        Dirty(ent);

        if (ent.Comp.DelayOptions[^1] <= ent.Comp.Delay)
        {
            ent.Comp.Delay = ent.Comp.DelayOptions[0];
            _popup.PopupEntity(Loc.GetString("timer-trigger-popup-set", ("time", ent.Comp.Delay)), ent.Owner, user);
            return;
        }

        foreach (var option in ent.Comp.DelayOptions)
        {
            if (option > ent.Comp.Delay)
            {
                ent.Comp.Delay = option;
                _popup.PopupEntity(Loc.GetString("timer-trigger-popup-set", ("time", option)), ent.Owner, user);
                return;
            }
        }
    }

    private void OnRepeatingTimer(Entity<RepeatingTriggerComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != RepeatingTriggerTimer)
            return;

        ent.Comp.NextTrigger = args.NextDeadline ?? args.ScheduledTime + ent.Comp.Delay;
        Dirty(ent);
        Trigger(ent, null, ent.Comp.KeyOut);
    }

    private void OnActiveTimer(Entity<ActiveTimerTriggerComponent> ent, ref EntityTimerEvent args)
    {
        if (!TryComp<TimerTriggerComponent>(ent, out var timer))
            return;

        if (args.Id == TimerTriggerBeepTimer)
        {
            if (_net.IsServer && timer.BeepSound != null)
                _audio.PlayPvs(timer.BeepSound, ent);

            timer.NextBeep = args.NextDeadline ?? timer.NextBeep + timer.BeepInterval;
            return;
        }

        if (args.Id != TimerTriggerTimer)
            return;

        var user = TerminatingOrDeleted(timer.User) ? null : timer.User;
        Trigger(ent, user, timer.KeyOut);
        // Remove after triggering to prevent it from starting the timer again.
        RemComp<ActiveTimerTriggerComponent>(ent);
        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, TriggerVisuals.VisualState, TriggerVisualState.Unprimed, appearance);
    }

    private void ScheduleRepeatingTimer(Entity<RepeatingTriggerComponent> ent)
    {
        if (ent.Comp.Delay <= TimeSpan.Zero)
            return;

        _entityTimers.SetTimerAt(
            ent,
            RepeatingTriggerTimer,
            ent.Comp.NextTrigger,
            ent.Comp.Delay);
    }

    private void ScheduleActiveTimer(EntityUid uid, TimerTriggerComponent timer)
    {
        if (!TryComp<ActiveTimerTriggerComponent>(uid, out var active))
            return;

        _entityTimers.SetTimerAt<ActiveTimerTriggerComponent>(
            (uid, active),
            TimerTriggerTimer,
            timer.NextTrigger);

        if (!_net.IsServer || timer.BeepSound == null || timer.BeepInterval <= TimeSpan.Zero)
            return;

        _entityTimers.SetTimerAt<ActiveTimerTriggerComponent>(
            (uid, active),
            TimerTriggerBeepTimer,
            timer.NextBeep,
            timer.BeepInterval);
    }
}
