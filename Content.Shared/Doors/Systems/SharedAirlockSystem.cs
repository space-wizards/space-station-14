using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Wires;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

public abstract class SharedAirlockSystem : EntitySystem
{
    [Dependency] private   readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedDoorSystem DoorSystem = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UISystem = default!;
    [Dependency] private   readonly SharedWiresSystem _wiresSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<AirlockComponent, DoorStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<AirlockComponent, DoorBoltChangedEvent>(OnBoltsChanged);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
        SubscribeLocalEvent<AirlockComponent, GetPryTimeModifierEvent>(OnGetPryMod);
        SubscribeLocalEvent<AirlockComponent, BeforePryEvent>(OnBeforePry);

        SubscribeLocalEvent<AirlockComponent, ActivateInWorldEvent>(OnAirlockActivate);
        SubscribeLocalEvent<AirlockComponent, InteractUsingEvent>(OnAirlockInteractUsing);
    }

    private void OnAirlockActivate(Entity<AirlockComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (ent.Comp.KeepOpenIfClicked && ent.Comp.AutoClose)
        {
            ent.Comp.AutoClose = false;
            Dirty(ent);
        }
    }

    private void OnAirlockInteractUsing(Entity<AirlockComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_wiresSystem.CanWireAct(args.Used))
            return;

        args.Handled = true;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panel) &&
            panel.Open)
        {
            if (TryComp<WiresPanelSecurityComponent>(ent.Owner, out var wiresPanelSecurity) &&
                !wiresPanelSecurity.WiresAccessible)
            {
                return;
            }

            UISystem.TryToggleUi(ent.Owner, WiresUiKey.Key, args.User);
        }
    }

    private void OnBeforeDoorClosed(EntityUid uid, AirlockComponent airlock, BeforeDoorClosedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!airlock.Safety)
            args.PerformCollisionCheck = false;

        // only block based on bolts / power status when initially closing the door, not when its already
        // mid-transition. Particularly relevant for when the door was pried-closed with a crowbar, which bypasses
        // the initial power-check.

        if (TryComp(uid, out DoorComponent? door)
            && !args.Partial
            && !CanChangeState(uid, airlock))
        {
            args.Cancel();
        }
    }

    private void OnStateChanged(EntityUid uid, AirlockComponent component, DoorStateChangedEvent args)
    {
        // This is here so we don't accidentally bulldoze state values and mispredict.
        if (_timing.ApplyingState)
            return;

        // Only show the maintenance panel if the airlock is closed
        if (TryComp<WiresPanelComponent>(uid, out var wiresPanel))
        {
            _wiresSystem.ChangePanelVisibility(uid, wiresPanel, component.OpenPanelVisible || args.State != DoorState.Open);
        }
        // If the door is closed, we should look if the bolt was locked while closing
        UpdateAutoClose(uid, component);

        // Make sure the airlock auto closes again next time it is opened
        if (args.State == DoorState.Closed)
        {
            component.AutoClose = true;
            Dirty(uid, component);
        }
    }

    private void OnBoltsChanged(EntityUid uid, AirlockComponent component, DoorBoltChangedEvent args)
    {
        // If unbolted, reset the auto close timer
        if (!args.Bolted)
            UpdateAutoClose(uid, component);
        else
            DoorSystem.SetNextStateChange(uid, null);
    }

    private void OnBeforeDoorOpened(EntityUid uid, AirlockComponent component, BeforeDoorOpenedEvent args)
    {
        if (!CanChangeState(uid, component))
            args.Cancel();
    }

    private void OnBeforeDoorDenied(EntityUid uid, AirlockComponent component, BeforeDoorDeniedEvent args)
    {
        if (!CanChangeState(uid, component))
            args.Cancel();
    }

    private void OnGetPryMod(EntityUid uid, AirlockComponent component, ref GetPryTimeModifierEvent args)
    {
        if (component.Powered)
            args.PryTimeModifier *= component.PoweredPryModifier;

        if (DoorSystem.IsBolted(uid))
            args.PryTimeModifier *= component.BoltedPryModifier;
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
        RaiseLocalEvent(uid, autoev);
        if (autoev.Cancelled)
            return;

        DoorSystem.SetNextStateChange(uid, airlock.AutoCloseDelay * airlock.AutoCloseDelayModifier);
    }

    private void OnBeforePry(EntityUid uid, AirlockComponent component, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (!component.Powered || args.PryPowered)
            return;

        args.Message = "airlock-component-cannot-pry-is-powered-message";

        args.Cancelled = true;
    }

    public void UpdateEmergencyLightStatus(EntityUid uid, AirlockComponent component)
    {
        Appearance.SetData(uid, DoorVisuals.EmergencyLights, component.EmergencyAccess);
    }

    public void SetEmergencyAccess(Entity<AirlockComponent> ent, bool value, EntityUid? user = null, bool predicted = false)
    {
        if(!ent.Comp.Powered)
            return;

        if (ent.Comp.EmergencyAccess == value)
            return;

        ent.Comp.EmergencyAccess = value;
        Dirty(ent, ent.Comp); // This only runs on the server apparently so we need this.
        UpdateEmergencyLightStatus(ent, ent.Comp);

        var sound = ent.Comp.EmergencyAccess ? ent.Comp.EmergencyOnSound : ent.Comp.EmergencyOffSound;
        if (predicted)
            Audio.PlayPredicted(sound, ent, user: user);
        else
            Audio.PlayPvs(sound, ent);
    }

    public void SetAutoCloseDelayModifier(AirlockComponent component, float value)
    {
        if (component.AutoCloseDelayModifier.Equals(value))
            return;

        component.AutoCloseDelayModifier = value;
    }

    public void SetSafety(AirlockComponent component, bool value)
    {
        component.Safety = value;
    }

    public bool CanChangeState(EntityUid uid, AirlockComponent component)
    {
        return component.Powered && !DoorSystem.IsBolted(uid);
    }
}
