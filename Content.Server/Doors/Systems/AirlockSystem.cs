using Content.Server.DeviceLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Wires;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.Wires;
using Content.Shared.Prying.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Doors.Systems;

public sealed class AirlockSystem : SharedAirlockSystem
{
    [Dependency] private readonly WiresSystem _wiresSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly DoorBoltSystem _bolts = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, ComponentInit>(OnAirlockInit);
        SubscribeLocalEvent<AirlockComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<AirlockComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AirlockComponent, DoorStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
        SubscribeLocalEvent<AirlockComponent, ActivateInWorldEvent>(OnActivate, before: new[] { typeof(DoorSystem) });
        SubscribeLocalEvent<AirlockComponent, GetPryTimeModifierEvent>(OnGetPryMod);
        SubscribeLocalEvent<AirlockComponent, BeforePryEvent>(OnBeforePry);

    }

    private void OnAirlockInit(EntityUid uid, AirlockComponent component, ComponentInit args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiverComponent))
        {
            Appearance.SetData(uid, DoorVisuals.Powered, receiverComponent.Powered);
        }
    }

    private void OnSignalReceived(EntityUid uid, AirlockComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.AutoClosePort)
        {
            component.AutoClose = false;
        }
    }

    private void OnPowerChanged(EntityUid uid, AirlockComponent component, ref PowerChangedEvent args)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearanceComponent))
        {
            Appearance.SetData(uid, DoorVisuals.Powered, args.Powered, appearanceComponent);
        }

        if (!TryComp(uid, out DoorComponent? door))
            return;

        if (!args.Powered)
        {
            // stop any scheduled auto-closing
            if (door.State == DoorState.Open)
                DoorSystem.SetNextStateChange(uid, null);
        }
        else
        {
            UpdateAutoClose(uid, door: door);
        }
    }

    private void OnStateChanged(EntityUid uid, AirlockComponent component, DoorStateChangedEvent args)
    {
        // TODO move to shared? having this be server-side, but having client-side door opening/closing & prediction
        // means that sometimes the panels & bolt lights may be visible despite a door being completely open.

        // Only show the maintenance panel if the airlock is closed
        if (TryComp<WiresPanelComponent>(uid, out var wiresPanel))
        {
            _wiresSystem.ChangePanelVisibility(uid, wiresPanel, component.OpenPanelVisible || args.State != DoorState.Open);
        }
        // If the door is closed, we should look if the bolt was locked while closing
        UpdateAutoClose(uid, component);

        // Make sure the airlock auto closes again next time it is opened
        if (args.State == DoorState.Closed)
            component.AutoClose = true;
    }

    /// <summary>
    /// Updates the auto close timer.
    /// </summary>
    public void UpdateAutoClose(EntityUid uid, AirlockComponent? airlock = null, DoorComponent? door = null)
    {
        if (!Resolve(uid, ref airlock, ref door))
            return;

        if (door.State != DoorState.Open)
            return;

        if (!airlock.AutoClose)
            return;

        if (!CanChangeState(uid, airlock))
            return;

        var autoev = new BeforeDoorAutoCloseEvent();
        RaiseLocalEvent(uid, autoev, false);
        if (autoev.Cancelled)
            return;

        DoorSystem.SetNextStateChange(uid, airlock.AutoCloseDelay * airlock.AutoCloseDelayModifier);
    }

    private void OnBeforeDoorOpened(EntityUid uid, AirlockComponent component, BeforeDoorOpenedEvent args)
    {
        if (!CanChangeState(uid, component))
            args.Cancel();
    }

    protected override void OnBeforeDoorClosed(EntityUid uid, AirlockComponent component, BeforeDoorClosedEvent args)
    {
        base.OnBeforeDoorClosed(uid, component, args);

        if (args.Cancelled)
            return;

        // only block based on bolts / power status when initially closing the door, not when its already
        // mid-transition. Particularly relevant for when the door was pried-closed with a crowbar, which bypasses
        // the initial power-check.

        if (TryComp(uid, out DoorComponent? door)
            && !door.Partial
            && !CanChangeState(uid, component))
        {
            args.Cancel();
        }
    }

    private void OnBeforeDoorDenied(EntityUid uid, AirlockComponent component, BeforeDoorDeniedEvent args)
    {
        if (!CanChangeState(uid, component))
            args.Cancel();
    }

    private void OnActivate(EntityUid uid, AirlockComponent component, ActivateInWorldEvent args)
    {
        if (TryComp<WiresPanelComponent>(uid, out var panel) &&
            panel.Open &&
            _prototypeManager.TryIndex<WiresPanelSecurityLevelPrototype>(panel.CurrentSecurityLevelID, out var securityLevelPrototype) &&
            securityLevelPrototype.WiresAccessible &&
            TryComp<ActorComponent>(args.User, out var actor))
        {
            _wiresSystem.OpenUserInterface(uid, actor.PlayerSession);
            args.Handled = true;
            return;
        }

        if (component.KeepOpenIfClicked)
        {
            // Disable auto close
            component.AutoClose = false;
        }
    }

    private void OnGetPryMod(EntityUid uid, AirlockComponent component, ref GetPryTimeModifierEvent args)
    {
        if (_power.IsPowered(uid))
            args.PryTimeModifier *= component.PoweredPryModifier;
    }

    private void OnBeforePry(EntityUid uid, AirlockComponent component, ref BeforePryEvent args)
    {
        if (this.IsPowered(uid, EntityManager) && !args.PryPowered)
        {
            Popup.PopupClient(Loc.GetString("airlock-component-cannot-pry-is-powered-message"), uid, args.User);
            args.Cancelled = true;
        }
    }

    public bool CanChangeState(EntityUid uid, AirlockComponent component)
    {
        return this.IsPowered(uid, EntityManager) && !_bolts.IsBolted(uid);
    }
}
