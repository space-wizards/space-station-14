using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.MassDriver.Components;
using Content.Shared.MassDriver.EntitySystems;
using Content.Shared.MassDriver;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Utility;
using Robust.Shared.GameStates;

namespace Content.Server.MassDriver.EntitySystems;

public sealed class MassDriverSystem : SharedMassDriverSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        // UI for console -_-
        SubscribeLocalEvent<MassDriverComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverModeMessage>(OnModeChanged);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverLaunchMessage>(OnLaunch);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverThrowSpeedMessage>(OnThrowSpeedChanged);
        SubscribeLocalEvent<MassDriverConsoleComponent, MassDriverThrowDistanceMessage>(OnThrowDistanceChanged);
        SubscribeLocalEvent<MassDriverConsoleComponent, BoundUIOpenedEvent>(OnUIOpen);

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

        driver.Console = uid;
        Dirty(args.Sink, driver);
        Dirty(uid, component);
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

        if (TryComp<MassDriverComponent>(args.Sink, out var driver))
        {
            driver.Console = null;
            Dirty(args.Sink, driver);
        }

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

    /// <summary>
    /// Change power load of mass driver
    /// </summary>
    public override void ChangePowerLoad(EntityUid uid, MassDriverComponent component, float powerLoad)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            _powerReceiver.SetLoad(receiver, powerLoad);
    }

    #endregion

    #region UI

    /// <summary>
    /// Handles the component state being requested from the server.
    /// </summary>
    private void OnGetState(EntityUid uid, MassDriverComponent component, ref ComponentGetState args)
    {
        args.State = new MassDriverComponentState()
        {
            MaxThrowSpeed = component.MaxThrowSpeed,
            MaxThrowDistance = component.MaxThrowDistance,
            MinThrowSpeed = component.MinThrowSpeed,
            MinThrowDistance = component.MinThrowDistance,
            CurrentThrowSpeed = component.CurrentThrowSpeed,
            CurrentThrowDistance = component.CurrentThrowDistance,
            CurrentMassDriverMode = component.Mode,
            Console = GetNetEntity(component.Console),
            Hacked = component.Hacked
        };
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
            {
                massDriverComponent.CurrentThrowSpeed = Math.Clamp(args.Speed, massDriverComponent.MinThrowSpeed, massDriverComponent.MaxThrowSpeed);
                Dirty(massDriverUid, massDriverComponent);
            }
    }

    /// <summary>
    /// Handle throw distance slider
    /// </summary>
    private void OnThrowDistanceChanged(EntityUid uid, MassDriverConsoleComponent component, MassDriverThrowDistanceMessage args)
    {
        foreach (var massDriverUid in component.MassDrivers)
            if (TryComp<MassDriverComponent>(massDriverUid, out var massDriverComponent))
            {
                massDriverComponent.CurrentThrowDistance = Math.Clamp(args.Distance, massDriverComponent.MinThrowDistance, massDriverComponent.MaxThrowDistance);
                Dirty(massDriverUid, massDriverComponent);
            }
    }

    /// <summary>
    /// Handles the UI being opened to send the current state to the UI.
    /// </summary>
    private void OnUIOpen(EntityUid uid, MassDriverConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!_ui.HasUi(uid, MassDriverConsoleUiKey.Key))
            return;

        if (!TryComp<MassDriverComponent>(component.MassDrivers.FirstOrNull(), out var massDriver))
            return;

        var state = new MassDriverComponentState()
        {
            MaxThrowSpeed = massDriver.MaxThrowSpeed,
            MaxThrowDistance = massDriver.MaxThrowDistance,
            MinThrowSpeed = massDriver.MinThrowSpeed,
            MinThrowDistance = massDriver.MinThrowDistance,
            CurrentThrowSpeed = massDriver.CurrentThrowSpeed,
            CurrentThrowDistance = massDriver.CurrentThrowDistance,
            CurrentMassDriverMode = massDriver.Mode,
            Console = GetNetEntity(massDriver.Console),
            Hacked = massDriver.Hacked
        };

        _ui.ServerSendUiMessage(uid, MassDriverConsoleUiKey.Key, new MassDriverUpdateUIMessage(state)); // Update UI on Open
    }

    #endregion
}
