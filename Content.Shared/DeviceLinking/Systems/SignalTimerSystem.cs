using Content.Shared.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.MachineLinking;
using Content.Shared.TextScreen;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class SignalTimerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private DeviceLinkSystem _signalSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;

    [Dependency] private EntityQuery<ActiveSignalTimerComponent> _activeTimerQuery = default!;

    /// <summary>
    /// Per-tick timer cache.
    /// </summary>
    private readonly List<Entity<SignalTimerComponent>> _timers = new();

    [SubscribeLocalEvent]
    private void OnInit(Entity<SignalTimerComponent> ent, ref ComponentInit args)
    {
        _appearanceSystem.SetData(ent.Owner, TextScreenVisuals.DefaultText, ent.Comp.Label);
        _appearanceSystem.SetData(ent.Owner, TextScreenVisuals.ScreenText, ent.Comp.Label);
        _signalSystem.EnsureSinkPort(ent.Owner, ent.Comp.Trigger);
    }

    [SubscribeLocalEvent]
    private void OnAfterActivatableUIOpen(Entity<SignalTimerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        var time = _activeTimerQuery.TryComp(ent.Owner, out var active) ? active.TriggerTime : TimeSpan.Zero;

        if (_ui.HasUi(ent.Owner, SignalTimerUiKey.Key))
        {
            _ui.SetUiState(ent.Owner,
                SignalTimerUiKey.Key,
                new SignalTimerBoundUserInterfaceState(ent.Comp.Label,
                TimeSpan.FromSeconds(ent.Comp.Delay).Minutes.ToString("D2"),
                TimeSpan.FromSeconds(ent.Comp.Delay).Seconds.ToString("D2"),
                ent.Comp.CanEditLabel,
                time,
                active != null,
                _accessReader.IsAllowed(args.User, ent.Owner)));
        }
    }

    /// <summary>
    ///     Finishes a timer, triggering its main port, and removing its <see cref="ActiveSignalTimerComponent"/>.
    /// </summary>
    public void Trigger(Entity<SignalTimerComponent> ent)
    {
        RemComp<ActiveSignalTimerComponent>(ent.Owner);

        _audio.PlayPvs(ent.Comp.DoneSound, ent.Owner);
        _signalSystem.InvokePort(ent.Owner, ent.Comp.TriggerPort);

        if (_ui.HasUi(ent.Owner, SignalTimerUiKey.Key))
        {
            _ui.SetUiState(ent.Owner,
                SignalTimerUiKey.Key,
                new SignalTimerBoundUserInterfaceState(ent.Comp.Label,
                TimeSpan.FromSeconds(ent.Comp.Delay).Minutes.ToString("D2"),
                TimeSpan.FromSeconds(ent.Comp.Delay).Seconds.ToString("D2"),
                ent.Comp.CanEditLabel,
                TimeSpan.Zero,
                false,
                true));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        _timers.Clear();

        var query = EntityQueryEnumerator<ActiveSignalTimerComponent, SignalTimerComponent>();
        while (query.MoveNext(out var uid, out var active, out var timer))
        {
            if (active.TriggerTime > _gameTiming.CurTime)
                continue;

            _timers.Add((uid, timer));
        }

        foreach (var timer in _timers)
        {
            // Exploded or the likes.
            if (!Exists(timer.Owner))
                continue;

            Trigger(timer);
        }
    }

    /// <summary>
    ///     Checks if a UI <paramref name="message"/> is allowed to be sent by the user.
    /// </summary>
    /// <param name="uid">The entity that is interacted with.</param>
    private bool IsMessageValid(EntityUid uid, BoundUserInterfaceMessage message)
    {
        return _accessReader.IsAllowed(message.Actor, uid);
    }

    /// <summary>
    ///     Called by <see cref="SignalTimerTextChangedMessage"/> to both
    ///     change the default ent.Comp label, and propagate that change to the TextScreen.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTextChangedMessage(Entity<SignalTimerComponent> ent, ref SignalTimerTextChangedMessage args)
    {
        if (!IsMessageValid(ent.Owner, args))
            return;

        ent.Comp.Label = args.Text[..Math.Min(ent.Comp.MaxLength, args.Text.Length)];

        if (_activeTimerQuery.HasComp(ent.Owner))
            return;

        // could maybe move the defaulttext update out of this block,
        // if you delved deep into appearance update batching
        _appearanceSystem.SetData(ent.Owner, TextScreenVisuals.DefaultText, ent.Comp.Label);
        _appearanceSystem.SetData(ent.Owner, TextScreenVisuals.ScreenText, ent.Comp.Label);
    }

    /// <summary>
    ///     Called by <see cref="SignalTimerDelayChangedMessage"/> to change the <see cref="SignalTimerComponent"/>
    ///     delay, and propagate that change to a textscreen.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDelayChangedMessage(Entity<SignalTimerComponent> ent, ref SignalTimerDelayChangedMessage args)
    {
        if (!IsMessageValid(ent.Owner, args))
            return;

        ent.Comp.Delay = Math.Min(args.Delay.TotalSeconds, ent.Comp.MaxDuration);
        _appearanceSystem.SetData(ent.Owner, TextScreenVisuals.TargetTime, ent.Comp.Delay);
    }

    /// <summary>
    ///     Called by <see cref="SignalTimerStartMessage"/> to instantiate an <see cref="ActiveSignalTimerComponent"/>,
    ///     clear <see cref="TextScreenVisuals.ScreenText"/>, propagate those changes, and invoke the start port.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTimerStartMessage(Entity<SignalTimerComponent> ent, ref SignalTimerStartMessage args)
    {
        if (!IsMessageValid(ent.Owner, args))
            return;

        // feedback received: pressing the timer button while a timer is running should cancel the timer.
        if (_activeTimerQuery.HasComp(ent.Owner))
        {
            _appearanceSystem.SetData(ent.Owner, TextScreenVisuals.TargetTime, _gameTiming.CurTime);
            Trigger(ent);
        }
        else
            OnStartTimer(ent);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<SignalTimerComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.Trigger)
        {
            OnStartTimer(ent);
        }
    }

    public void OnStartTimer(Entity<SignalTimerComponent> ent)
    {
        var timer = EnsureComp<ActiveSignalTimerComponent>(ent);
        timer.TriggerTime = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp.Delay);

        _appearanceSystem.SetData(ent.Owner, TextScreenVisuals.TargetTime, timer.TriggerTime);
        _appearanceSystem.SetData(ent.Owner, TextScreenVisuals.ScreenText, string.Empty);

        _signalSystem.InvokePort(ent.Owner, ent.Comp.StartPort);
    }
}
