using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.MassDriver.Components;
using Content.Shared.MassDriver.EntitySystems;
using Content.Shared.MassDriver;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Utility;

namespace Content.Server.MassDriver.EntitySystems;

public sealed class MassDriverSystem : SharedMassDriverSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        // UI for console -_-
        SubscribeLocalEvent<MassDriverConsoleComponent, ComponentInit>(OnInit); // Update state on init
        SubscribeLocalEvent<MassDriverConsoleComponent, BoundUIOpenedEvent>(OnBoundUiOpened); // Update state on ui open
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverModeMessage>(OnModeChanged);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverLaunchMessage>(OnLaunch);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverThrowSpeedMessage>(OnThrowSpeedChanged);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverThrowDistanceMessage>(OnThrowDistanceChanged);

        // Device Linking
        SubscribeLocalEvent<MassDriverConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<MassDriverConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<MassDriverComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    #region DeviceLinking

    /// <summary>
    /// Handle new link to add mass driver in mass driver console component
    /// </summary>
    /// <param name="uid">Mass Driver Console</param>
    /// <param name="component">Mass Driver Console Component</param>
    /// <param name="args">Event arguments</param>
    private void OnNewLink(EntityUid uid, MassDriverConsoleComponent component, NewLinkEvent args)
    {
        if (!TryComp<MassDriverComponent>(args.Sink, out var driver))
            return;

        component.MassDrivers.Add(args.Sink);

        Dirty(args.Sink, driver);
        Dirty(uid, component);

        UpdateUserInterface(uid, component.MassDrivers.FirstOrNull());
    }

    /// <summary>
    /// Handle disconnecting to remove mass driver from mass driver console component
    /// </summary>
    /// <param name="uid">Mass Driver Console</param>
    /// <param name="component">Mass Driver Console Component</param>
    /// <param name="args">Event arguments</param>
    private void OnPortDisconnected(EntityUid uid, MassDriverConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port != component.LinkingPort || !component.MassDrivers.Contains(args.Sink))
            return;

        component.MassDrivers.Remove(args.Sink);
        Dirty(uid, component);
    }

    /// <summary>
    /// Handle signal receive, for launch some objects.
    /// </summary>
    /// <param name="uid">Mass Driver</param>
    /// <param name="component">Mass Driver Component</param>
    /// <param name="args">Event arguments</param>
    private void OnSignalReceived(EntityUid uid, MassDriverComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.LaunchPort && component.Mode == MassDriverMode.Manual)
            AddComp<ActiveMassDriverComponent>(uid);
    }

    #endregion

    #region Logic

    public override void ChangePowerLoad(EntityUid uid, MassDriverComponent component, float powerLoad)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            _powerReceiver.SetLoad(receiver, powerLoad);
    }

    #endregion

    #region UI

    /// <summary>
    /// Update ui on init
    /// </summary>
    private void OnInit(EntityUid uid, MassDriverConsoleComponent component, ComponentInit args)
    {
        UpdateUserInterface(uid, component.MassDrivers.FirstOrNull());
    }

    /// <summary>
    /// Update ui on ui open
    /// </summary>
    private void OnBoundUiOpened(EntityUid uid, MassDriverConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component.MassDrivers.FirstOrNull());
    }

    /// <summary>
    /// Handle mode changing
    /// </summary>
    private void OnModeChanged(EntityUid uid, MassDriverConsoleComponent component, MassDriverModeMessage args)
    {
        foreach (var massDriverNetEntity in component.MassDrivers)
        {
            var massDriverUid = massDriverNetEntity;

            if (!TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
                continue;

            massDriverComponent.Mode = args.Mode;
            Dirty(massDriverUid, massDriverComponent);

            if (massDriverComponent.Mode == MassDriverMode.Auto)
                EnsureComp<ActiveMassDriverComponent>(massDriverUid);
            RemComp<ActiveMassDriverComponent>(massDriverUid);
        }

        UpdateUserInterface(uid, component.MassDrivers.FirstOrNull());
    }

    /// <summary>
    /// Handle launch button, so we can launch entity
    /// </summary>
    private void OnLaunch(EntityUid uid, MassDriverConsoleComponent component, MassDriverLaunchMessage args)
    {
        foreach (var massDriverUid in component.MassDrivers)
            AddComp<ActiveMassDriverComponent>(massDriverUid);
    }

    /// <summary>
    /// Handle throw speed slider
    /// </summary>
    private void OnThrowSpeedChanged(EntityUid uid, MassDriverConsoleComponent component, MassDriverThrowSpeedMessage args)
    {
        foreach (var massDriverUid in component.MassDrivers)
            if (TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
                massDriverComponent.CurrentThrowSpeed = Math.Clamp(args.Speed, massDriverComponent.MinThrowSpeed, massDriverComponent.MaxThrowSpeed);
    }

    /// <summary>
    /// Handle throw distance slider
    /// </summary>
    private void OnThrowDistanceChanged(EntityUid uid, MassDriverConsoleComponent component, MassDriverThrowDistanceMessage args)
    {
        foreach (var massDriverUid in component.MassDrivers)
            if (TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
                massDriverComponent.CurrentThrowDistance = Math.Clamp(args.Distance, massDriverComponent.MinThrowDistance, massDriverComponent.MaxThrowDistance);
    }

    private void UpdateUserInterface(EntityUid console, EntityUid? massDriver, MassDriverComponent? component = null)
    {
        if (!_ui.HasUi(console, MassDriverConsoleUiKey.Key))
            return;

        MassDriverComponentState state;

        if (massDriver != null && Resolve(massDriver.Value, ref component))
        {
            state = new MassDriverComponentState
            {
                MaxThrowSpeed = component.MaxThrowSpeed,
                MaxThrowDistance = component.MaxThrowDistance,
                MinThrowSpeed = component.MinThrowSpeed,
                MinThrowDistance = component.MinThrowDistance,
                CurrentThrowSpeed = component.CurrentThrowSpeed,
                CurrentThrowDistance = component.CurrentThrowDistance,
                CurrentMassDriverMode = component.Mode,
                MassDriverLinked = true

            };
        }
        else
        {
            state = new MassDriverComponentState
            {
                MassDriverLinked = false
            };
        }

        _ui.ServerSendUiMessage(console, MassDriverConsoleUiKey.Key, new MassDriverUpdateUIMessage(state));
    }

    #endregion
}
