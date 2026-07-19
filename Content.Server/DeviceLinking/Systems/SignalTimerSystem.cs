using Content.Server.DeviceLinking.Components;
using Content.Shared.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.MachineLinking;
using Content.Shared.TextScreen;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeviceLinking.Systems;

public sealed partial class SignalTimerSystem : EntitySystem
{
    private static readonly EntityTimerId SignalTimer = new("signal-timer");

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private EntityTimerSystem _timers = default!;
    [Dependency] private DeviceLinkSystem _signalSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignalTimerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignalTimerComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);

        SubscribeLocalEvent<SignalTimerComponent, SignalTimerTextChangedMessage>(OnTextChangedMessage);
        SubscribeLocalEvent<SignalTimerComponent, SignalTimerDelayChangedMessage>(OnDelayChangedMessage);
        SubscribeLocalEvent<SignalTimerComponent, SignalTimerStartMessage>(OnTimerStartMessage);
        SubscribeLocalEvent<SignalTimerComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ActiveSignalTimerComponent, ComponentInit>(OnActiveTimerInit);
        SubscribeLocalEvent<ActiveSignalTimerComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnInit(EntityUid uid, SignalTimerComponent component, ComponentInit args)
    {
        _appearanceSystem.SetData(uid, TextScreenVisuals.DefaultText, component.Label);
        _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Label);
        _signalSystem.EnsureSinkPorts(uid, component.Trigger);
    }

    private void OnAfterActivatableUIOpen(EntityUid uid, SignalTimerComponent component, AfterActivatableUIOpenEvent args)
    {
        var time = TryComp<ActiveSignalTimerComponent>(uid, out var active) ? active.TriggerTime : TimeSpan.Zero;

        if (_ui.HasUi(uid, SignalTimerUiKey.Key))
        {
            _ui.SetUiState(uid, SignalTimerUiKey.Key, new SignalTimerBoundUserInterfaceState(component.Label,
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
            _ui.SetUiState(uid, SignalTimerUiKey.Key, new SignalTimerBoundUserInterfaceState(signalTimer.Label,
                TimeSpan.FromSeconds(signalTimer.Delay).Minutes.ToString("D2"),
                TimeSpan.FromSeconds(signalTimer.Delay).Seconds.ToString("D2"),
                signalTimer.CanEditLabel,
                TimeSpan.Zero,
                false,
                true));
        }
    }

    private void OnActiveTimerInit(Entity<ActiveSignalTimerComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.TriggerTime != TimeSpan.Zero)
            Schedule(ent);
    }

    private void OnTimer(Entity<ActiveSignalTimerComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != SignalTimer || !TryComp<SignalTimerComponent>(ent, out var timer))
            return;

        Trigger(ent, timer);
    }

    /// <summary>
    ///     Checks if a UI <paramref name="message"/> is allowed to be sent by the user.
    /// </summary>
    /// <param name="uid">The entity that is interacted with.</param>
    private bool IsMessageValid(EntityUid uid, BoundUserInterfaceMessage message)
    {
        if (!_accessReader.IsAllowed(message.Actor, uid))
            return false;

        return true;
    }

    /// <summary>
    ///     Called by <see cref="SignalTimerTextChangedMessage"/> to both
    ///     change the default component label, and propagate that change to the TextScreen.
    /// </summary>
    private void OnTextChangedMessage(EntityUid uid, SignalTimerComponent component, SignalTimerTextChangedMessage args)
    {
        if (!IsMessageValid(uid, args))
            return;

        component.Label = args.Text[..Math.Min(component.MaxLength, args.Text.Length)];

        if (!HasComp<ActiveSignalTimerComponent>(uid))
        {
            // could maybe move the defaulttext update out of this block,
            // if you delved deep into appearance update batching
            _appearanceSystem.SetData(uid, TextScreenVisuals.DefaultText, component.Label);
            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Label);
        }
    }

    /// <summary>
    ///     Called by <see cref="SignalTimerDelayChangedMessage"/> to change the <see cref="SignalTimerComponent"/>
    ///     delay, and propagate that change to a textscreen.
    /// </summary>
    private void OnDelayChangedMessage(EntityUid uid, SignalTimerComponent component, SignalTimerDelayChangedMessage args)
    {
        if (!IsMessageValid(uid, args))
            return;

        component.Delay = Math.Min(args.Delay.TotalSeconds, component.MaxDuration);
        _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, component.Delay);
    }

    /// <summary>
    ///     Called by <see cref="SignalTimerStartMessage"/> to instantiate an <see cref="ActiveSignalTimerComponent"/>,
    ///     clear <see cref="TextScreenVisuals.ScreenText"/>, propagate those changes, and invoke the start port.
    /// </summary>
    private void OnTimerStartMessage(EntityUid uid, SignalTimerComponent component, SignalTimerStartMessage args)
    {
        if (!IsMessageValid(uid, args))
            return;

        // feedback received: pressing the timer button while a timer is running should cancel the timer.
        if (HasComp<ActiveSignalTimerComponent>(uid))
        {
            _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, _gameTiming.CurTime);
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

    public void OnStartTimer(EntityUid uid, SignalTimerComponent component)
    {
        TryComp<AppearanceComponent>(uid, out var appearance);
        var timer = EnsureComp<ActiveSignalTimerComponent>(uid);
        timer.TriggerTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Delay);
        Schedule((uid, timer));

        if (appearance != null)
        {
            _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, timer.TriggerTime, appearance);
            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, string.Empty, appearance);
        }

        _signalSystem.InvokePort(uid, component.StartPort);
    }

    private void Schedule(Entity<ActiveSignalTimerComponent> ent)
    {
        _timers.SetTimerAt(ent, SignalTimer, ent.Comp.TriggerTime);
    }
}
