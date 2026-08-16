using Content.Server.DeviceLinking.Components;
using Content.Shared.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.TextScreen.Components;
using Content.Shared.TextScreen.Systems;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// A system for signallable timers. This is a timer with a screen
/// </summary>
/// <seealso cref="TextScreenTimerComponent"/>
public sealed partial class SignalTimerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private TextScreenSystem _textScreen = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    [Dependency] private EntityQuery<TextScreenTimerComponent> _timerScreenQuery = default!;
    [Dependency] private EntityQuery<ActiveSignalTimerComponent> _activeTimerQuery = default!;

    /// <summary>
    /// Per-tick timer cache.
    /// </summary>
    private List<Entity<SignalTimerComponent>> _timers = new();

    #region Event Handlers
    [SubscribeLocalEvent]
    private void OnInit(Entity<SignalTimerComponent> ent, ref ComponentInit args)
    {
        if (!_timerScreenQuery.TryComp(ent, out var textScreen))
            _textScreen.SetTimerStrings((ent, textScreen), ent.Comp.Label);

        _deviceLink.EnsureSinkPorts(ent, ent.Comp.Trigger);
    }

    [SubscribeLocalEvent]
    private void OnAfterActivatableUIOpen(Entity<SignalTimerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        var time = _activeTimerQuery.TryComp(ent, out var active) ? active.TriggerTime : TimeSpan.Zero;

        if (!_ui.HasUi(ent, SignalTimerUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, SignalTimerUiKey.Key, new SignalTimerBoundUserInterfaceState(ent.Comp.Label,
            TimeSpan.FromSeconds(ent.Comp.Delay).Minutes.ToString("D2"),
            TimeSpan.FromSeconds(ent.Comp.Delay).Seconds.ToString("D2"),
            ent.Comp.CanEditLabel,
            time,
            active != null,
            _accessReader.IsAllowed(args.User, ent)));
    }

    /// <summary>
    /// Called by <see cref="SignalTimerTextChangedMessage"/> to both
    /// change the default component label, and propagate that change to the TextScreen.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTextChangedMessage(Entity<SignalTimerComponent> ent, ref SignalTimerTextChangedMessage args)
    {
        if (!IsMessageValid(ent, args))
            return;

        ent.Comp.Label = args.Text[..Math.Min(ent.Comp.MaxLength, args.Text.Length)];

        if (_activeTimerQuery.HasComp(ent))
            return;

        if (!_timerScreenQuery.TryComp(ent, out var textScreen))
            _textScreen.SetTimerStrings((ent, textScreen), ent.Comp.Label);
    }

    /// <summary>
    /// Called by <see cref="SignalTimerDelayChangedMessage"/> to change the <see cref="SignalTimerComponent"/>
    /// delay, and propagate that change to a textscreen.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDelayChangedMessage(Entity<SignalTimerComponent> ent, ref SignalTimerDelayChangedMessage args)
    {
        if (!IsMessageValid(ent, args))
            return;

        ent.Comp.Delay = Math.Min(args.Delay.TotalSeconds, ent.Comp.MaxDuration);
        if (_timerScreenQuery.TryComp(ent, out var textScreen))
            _textScreen.SetTimerTarget((ent, textScreen), _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp.Delay));
    }

    /// <summary>
    /// Called by <see cref="SignalTimerStartMessage"/> to instantiate an <see cref="ActiveSignalTimerComponent"/>,
    /// clear <see cref="TextScreenVisuals.ScreenText"/>, propagate those changes, and invoke the start port.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTimerStartMessage(Entity<SignalTimerComponent> ent, ref SignalTimerStartMessage args)
    {
        if (!IsMessageValid(ent, args))
            return;

        // feedback received: pressing the timer button while a timer is running should cancel the timer.
        if (_activeTimerQuery.HasComp(ent))
            Trigger(ent);
        else
            StartTimer(ent);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<SignalTimerComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.Trigger)
            StartTimer(ent);
    }
    #endregion Event Handlers

    #region Public API
    /// <summary>
    /// Finishes a timer, triggering its main port, and removing its <see cref="ActiveSignalTimerComponent"/>.
    /// </summary>
    public void Trigger(Entity<SignalTimerComponent> ent)
    {
        RemComp<ActiveSignalTimerComponent>(ent);

        _audio.PlayPvs(ent.Comp.DoneSound, ent);
        _deviceLink.InvokePort(ent, ent.Comp.TriggerPort);

        if (_timerScreenQuery.TryComp(ent, out var textScreen))
            _textScreen.SetTimerTarget((ent, textScreen), _gameTiming.CurTime);

        if (_ui.HasUi(ent, SignalTimerUiKey.Key))
        {
            _ui.SetUiState(ent.Owner, SignalTimerUiKey.Key, new SignalTimerBoundUserInterfaceState(ent.Comp.Label,
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

    public void StartTimer(Entity<SignalTimerComponent> ent)
    {
        var timer = EnsureComp<ActiveSignalTimerComponent>(ent);
        timer.TriggerTime = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp.Delay);

        if (_timerScreenQuery.TryComp(ent, out var textScreen))
            _textScreen.SetTimerTarget((ent, textScreen), timer.TriggerTime);

        _deviceLink.InvokePort(ent, ent.Comp.StartPort);
    }
    #endregion Public API

    #region Internal
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
    /// Checks if a UI <paramref name="message"/> is allowed to be sent by the user.
    /// </summary>
    /// <param name="uid">The entity that is interacted with.</param>
    private bool IsMessageValid(EntityUid uid, BoundUserInterfaceMessage message)
    {
        if (!_accessReader.IsAllowed(message.Actor, uid))
            return false;

        return true;
    }
    #endregion Internal
}
