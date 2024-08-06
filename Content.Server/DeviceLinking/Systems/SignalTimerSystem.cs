using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Shared.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.MachineLinking;
using Content.Shared.Screen;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeviceLinking.Systems;

public sealed class SignalTimerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    /// <summary>
    /// Per-tick timer cache.
    /// </summary>
    private List<Entity<SignalTimerComponent>> _timers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignalTimerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignalTimerComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);

        SubscribeLocalEvent<SignalTimerComponent, SignalTimerTextChangedMessage>(OnTextChangedMessage);
        SubscribeLocalEvent<SignalTimerComponent, SignalTimerDelayChangedMessage>(OnDelayChangedMessage);
        SubscribeLocalEvent<SignalTimerComponent, SignalTimerStartMessage>(OnTimerStartMessage);
        SubscribeLocalEvent<SignalTimerComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, SignalTimerComponent component, ComponentInit args)
    {
        var update = new ScreenUpdate(GetNetEntity(uid), ScreenPriority.Brig, component.Label);
        _appearanceSystem.SetData(uid, ScreenVisuals.Update, update);
        _signalSystem.EnsureSinkPorts(uid, component.Trigger);
    }

    private void OnAfterActivatableUIOpen(EntityUid uid, SignalTimerComponent component, AfterActivatableUIOpenEvent args)
    {
        var time = TryComp<ActiveSignalTimerComponent>(uid, out var active) ? active.TriggerTime : TimeSpan.Zero;

        if (_ui.HasUi(uid, SignalTimerUiKey.Key))
        {
            _ui.SetUiState(uid, SignalTimerUiKey.Key, new SignalTimerBoundUserInterfaceState(
                component.Label,
                TimeSpan.FromSeconds(component.Delay).Minutes.ToString("D2"),
                TimeSpan.FromSeconds(component.Delay).Seconds.ToString("D2"),
                component.CanEditLabel,
                time,
                active != null,
                _accessReader.IsAllowed(args.User, uid)));
        }
    }

    /// <summary>
    ///     Finishes a timer, triggering its main port, and removing its <see cref="ActiveSignalTimerComponent"/>.
    /// </summary>
    public void Trigger(EntityUid uid, SignalTimerComponent signalTimer)
    {
        RemComp<ActiveSignalTimerComponent>(uid);

        _audio.PlayPvs(signalTimer.DoneSound, uid);
        _signalSystem.InvokePort(uid, signalTimer.TriggerPort);

        if (_ui.HasUi(uid, SignalTimerUiKey.Key))
        {
            _ui.SetUiState(uid, SignalTimerUiKey.Key, new SignalTimerBoundUserInterfaceState(
                signalTimer.Label,
                TimeSpan.FromSeconds(signalTimer.Delay).Minutes.ToString("D2"),
                TimeSpan.FromSeconds(signalTimer.Delay).Seconds.ToString("D2"),
                signalTimer.CanEditLabel,
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

            Trigger(timer.Owner, timer.Comp);
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
    ///     Handles <see cref="SignalTimerTextChangedMessage"/>. Both changes
    ///     the default component label, and propagates that change to the screen.
    /// </summary>
    /// <remarks>
    ///     This gets called *each* time a player edits a brig timer's label field
    /// </remarks>
    private void OnTextChangedMessage(EntityUid uid, SignalTimerComponent component, SignalTimerTextChangedMessage args)
    {
        if (!IsMessageValid(uid, args))
            return;

        component.Label = args.Text[..Math.Min(component.MaxLength, args.Text.Length)];

        var update = new ScreenUpdate(GetNetEntity(uid), ScreenPriority.Default, component.Label);
        _appearanceSystem.SetData(uid, ScreenVisuals.Update, update);
    }

    /// <summary>
    ///     Handles <see cref="SignalTimerDelayChangedMessage"/>. Changes the <see cref="SignalTimerComponent"/> delay
    /// </summary>
    private void OnDelayChangedMessage(EntityUid uid, SignalTimerComponent component, SignalTimerDelayChangedMessage args)
    {
        if (!IsMessageValid(uid, args))
            return;

        component.Delay = args.Delay.TotalSeconds;
    }

    /// <summary>
    ///     Handles <see cref="SignalTimerStartMessage"/>. Instantiates an <see cref="ActiveSignalTimerComponent"/>,
    ///     clears <see cref="ScreenVisuals.ScreenText"/>, propagates those changes, and invokes the start port.
    /// </summary>
    private void OnTimerStartMessage(EntityUid uid, SignalTimerComponent component, SignalTimerStartMessage args)
    {
        if (!IsMessageValid(uid, args))
            return;

        // feedback received: pressing the timer button while a timer is running should cancel the timer.
        if (HasComp<ActiveSignalTimerComponent>(uid))
        {
            var update = new ScreenUpdate(GetNetEntity(uid), ScreenPriority.Brig, null, TimeSpan.Zero);
            _appearanceSystem.SetData(uid, ScreenVisuals.Update, update);
            Trigger(uid, component);
        }
        else
            OnStartTimer(uid, component);
    }

    private void OnSignalReceived(EntityUid uid, SignalTimerComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.Trigger)
        {
            OnStartTimer(uid, component);
        }
    }

    /// <summary>
    ///     "Handles" the timer starting (there is no single event) by either port or UI input
    /// </summary>
    public void OnStartTimer(EntityUid uid, SignalTimerComponent component)
    {
        if (!HasComp<AppearanceComponent>(uid))
            return;

        var timer = EnsureComp<ActiveSignalTimerComponent>(uid);
        timer.TriggerTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Delay);

        var update = new ScreenUpdate(GetNetEntity(uid), ScreenPriority.Brig, null, timer.TriggerTime);
        _appearanceSystem.SetData(uid, ScreenVisuals.Update, update);

        _signalSystem.InvokePort(uid, component.StartPort);
    }
}
