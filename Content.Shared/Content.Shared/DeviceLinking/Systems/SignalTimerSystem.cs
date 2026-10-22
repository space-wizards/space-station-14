using Content.Shared.Access.Systems;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.MachineLinking;
using Content.Shared.TextScreen;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.DeviceLinking.Systems;

/// <summary>
/// A system for signallable timers. Sets timer AppearanceData when triggered to start/stop.
/// </summary>
/// <seealso cref="Content.Client.TextScreen.TimerVisualsComponent"/>
/// <seealso cref="AppearanceComponent"/>
public sealed partial class SignalTimerSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [Dependency] private EntityQuery<ActiveSignalTimerComponent> _activeTimerQuery;
    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;

    /// <summary>
    /// Per-tick timer cache.
    /// </summary>
    private readonly List<Entity<SignalTimerComponent>> _timers = new();

    [SubscribeLocalEvent]
    private void OnInit(Entity<SignalTimerComponent> ent, ref ComponentInit args)
    {
        if (_appearanceQuery.TryComp(ent, out var appearance))
        {
            _appearance.SetData(ent, TextScreenVisuals.DefaultText, ent.Comp.Label, appearance);
            _appearance.SetData(ent, TextScreenVisuals.ScreenText, ent.Comp.Label, appearance);
            _appearance.SetData(ent, TextScreenVisuals.ScreenTextTime, _gameTiming.CurTime, appearance);
        }

        _deviceLink.EnsureSinkPort(ent.Owner, ent.Comp.Trigger);
    }

    [SubscribeLocalEvent]
    private void OnAfterActivatableUIOpen(Entity<SignalTimerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnAfterState(Entity<SignalTimerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    /// <summary>
    /// Called by <see cref="SignalTimerTextChangedMessage"/> to both
    /// change the default ent.Comp label, and propagate that change to the TextScreen.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTextChangedMessage(Entity<SignalTimerComponent> ent, ref SignalTimerTextChangedMessage args)
    {
        if (!IsMessageValid(ent.Owner, args))
            return;

        ent.Comp.Label = args.Text[..Math.Min(ent.Comp.MaxLength, args.Text.Length)];
        DirtyField(ent.AsNullable(), nameof(SignalTimerComponent.Label));

        if (!_appearanceQuery.TryComp(ent, out var appearance))
            return;

        // could maybe move the defaulttext update out of this block,
        // if you delved deep into appearance update batching
        _appearance.SetData(ent, TextScreenVisuals.DefaultText, ent.Comp.Label, appearance);
        _appearance.SetData(ent, TextScreenVisuals.ScreenText, ent.Comp.Label, appearance);
        _appearance.SetData(ent, TextScreenVisuals.ScreenTextTime, _gameTiming.CurTime, appearance);
    }

    /// <summary>
    /// Called by <see cref="SignalTimerDelayChangedMessage"/> to change the <see cref="SignalTimerComponent"/>
    /// delay, and propagate that change to a textscreen.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDelayChangedMessage(Entity<SignalTimerComponent> ent, ref SignalTimerDelayChangedMessage args)
    {
        if (!IsMessageValid(ent.Owner, args))
            return;

        // TODO make TimeSpan math helpers to replace the thing below
        ent.Comp.Delay = TimeSpan.FromSeconds(Math.Min(args.Delay.TotalSeconds, ent.Comp.MaxDuration.TotalSeconds));
        DirtyField(ent.AsNullable(), nameof(SignalTimerComponent.Delay));
        _appearance.SetData(ent.Owner, TextScreenVisuals.TargetTime, ent.Comp.Delay);
    }

    /// <summary>
    /// Called by <see cref="SignalTimerStartMessage"/> to instantiate an <see cref="ActiveSignalTimerComponent"/>,
    /// clear <see cref="TextScreenVisuals.ScreenText"/>, propagate those changes, and invoke the start port.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTimerStartMessage(Entity<SignalTimerComponent> ent, ref SignalTimerStartMessage args)
    {
        if (!IsMessageValid(ent.Owner, args))
            return;

        // feedback received: pressing the timer button while a timer is running should cancel the timer.
        if (_activeTimerQuery.HasComp(ent.Owner))
        {
            _appearance.SetData(ent.Owner, TextScreenVisuals.TargetTime, _gameTiming.CurTime);
            Trigger(ent);
        }
        else
            StartTimer(ent);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<SignalTimerComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.Trigger)
            StartTimer(ent);
    }

    #region Public API

    /// <summary>
    /// Finishes a timer, triggering its main port, and removing its <see cref="ActiveSignalTimerComponent"/>.
    /// </summary>
    public void Trigger(Entity<SignalTimerComponent> ent)
    {
        RemComp<ActiveSignalTimerComponent>(ent.Owner);

        _audio.PlayPvs(ent.Comp.DoneSound, ent.Owner);
        _deviceLink.InvokePort(ent.Owner, ent.Comp.TriggerPort);

        UpdateUi(ent);
    }

    public void StartTimer(Entity<SignalTimerComponent> ent)
    {
        var timer = EnsureComp<ActiveSignalTimerComponent>(ent);
        timer.TriggerTime = _gameTiming.CurTime + ent.Comp.Delay;
        DirtyField(ent.Owner, timer, nameof(ActiveSignalTimerComponent.TriggerTime));

        if (_appearanceQuery.TryComp(ent, out var appearance))
        {
            _appearance.SetData(ent, TextScreenVisuals.TargetTime, timer.TriggerTime, appearance);
            _appearance.SetData(ent, TextScreenVisuals.ScreenText, string.Empty, appearance);
        }

        _deviceLink.InvokePort(ent.Owner, ent.Comp.StartPort);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateTimer();
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
    private bool IsMessageValid(EntityUid uid, BoundUserInterfaceMessage message)
    {
        return _accessReader.IsAllowed(message.Actor, uid);
    }

    /// <summary>
    /// Updates the BUI with a <see cref="SignalTimerUiKey"/> key.
    /// </summary>
    /// <param name="ent"></param>
    private void UpdateUi(Entity<SignalTimerComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, SignalTimerUiKey.Key, out var bui))
            bui.Update();
    }

    #endregion Internal
}
