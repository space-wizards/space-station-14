using Content.Shared.Doors.Components;
using Content.Shared.Prying.Components;
using Content.Shared.Wires;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{

    [Dependency] protected readonly SharedDoorSystem DoorSystem = default!;
    [Dependency] private readonly SharedWiresSystem _wiresSystem = default!;

    private void InitializeAirlock()
    {
        SubscribeLocalEvent<AirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<AirlockComponent, DoorStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<AirlockComponent, DoorBoltsChangedEvent>(OnBoltsChanged);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
        SubscribeLocalEvent<AirlockComponent, GetPryTimeModifierEvent>(OnGetPryMod);
        SubscribeLocalEvent<AirlockComponent, BeforePryEvent>(OnBeforePry);
    }

    private void OnBeforeDoorClosed(Entity<AirlockComponent> airlock, ref BeforeDoorClosedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!airlock.Comp.Safety)
            args.PerformCollisionCheck = false;

        // Only block based on bolts / power status when initially closing the door, not when its already
        // mid-transition. Particularly relevant for when the door was pried-closed with a crowbar, which bypasses
        // the initial power-check.

        if (!TryComp(airlock, out DoorComponent? door)
            || door.State is DoorState.Opening or DoorState.Closing
            || CanChangeState(airlock,
                door.State is DoorState.AttemptingCloseByPrying or DoorState.AttemptingOpenByPrying))
            return;

        args.Cancel();
    }

    private void OnStateChanged(Entity<AirlockComponent> airlock, ref DoorStateChangedEvent args)
    {
        // This is here, so we don't accidentally bulldoze state values and mis-predict.
        if (_gameTiming.ApplyingState)
            return;

        // Only show the maintenance panel if the airlock is closed
        if (TryComp<WiresPanelComponent>(airlock, out var wiresPanel))
        {
            _wiresSystem.ChangePanelVisibility(airlock,
                wiresPanel,
                airlock.Comp.OpenPanelVisible || args.State != DoorState.Open);
        }

        // If the door is closed, we should look if the bolt was locked while closing
        UpdateAutoClose(airlock);

        // Make sure the airlock auto closes again next time it is opened
        if (args.State != DoorState.Closed)
            return;

        airlock.Comp.AutoClose = true;
        Dirty(airlock);
    }

    private void OnBoltsChanged(Entity<AirlockComponent> airlock, ref DoorBoltsChangedEvent args)
    {
        // If unbolted, reset the auto close timer
        if (args.BoltsDown)
            return;

        UpdateAutoClose(airlock);
    }

    private void OnBeforeDoorOpened(Entity<AirlockComponent> airlock, ref BeforeDoorOpenedEvent args)
    {
        if (CanChangeState(airlock))
            return;

        args.Cancel();
    }

    private void OnBeforeDoorDenied(Entity<AirlockComponent> airlock, ref BeforeDoorDeniedEvent args)
    {
        if (CanChangeState(airlock))
            return;

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
    protected void UpdateAutoClose(Entity<AirlockComponent> airlock, DoorComponent? door = null)
    {
        if (!Resolve(airlock, ref door))
            return;

        if (door.State is not DoorState.Open)
            return;

        if (!airlock.Comp.AutoClose)
            return;

        if (!CanChangeState(airlock))
            return;

        var autoCloseEvent = new BeforeDoorAutoCloseEvent();
        RaiseLocalEvent(airlock, autoCloseEvent);

        if (autoCloseEvent.Cancelled)
            return;

        DoorSystem.SetNextStateChange((airlock, door),
            airlock.Comp.AutoCloseDelay * airlock.Comp.AutoCloseDelayModifier);
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

    public void SetEmergencyAccess(Entity<AirlockComponent> airlock,
        bool value,
        EntityUid? user = null,
        bool predicted = false)
    {
        if (!airlock.Comp.Powered)
            return;

        if (airlock.Comp.EmergencyAccess == value)
            return;

        airlock.Comp.EmergencyAccess = value;
        _appearance.SetData(airlock, DoorVisuals.EmergencyLights, airlock.Comp.EmergencyAccess);

        Dirty(airlock, airlock.Comp);

        var sound = airlock.Comp.EmergencyAccess ? airlock.Comp.EmergencyOnSound : airlock.Comp.EmergencyOffSound;
        if (predicted)
            _audio.PlayPredicted(sound, airlock, user: user);
        else
            _audio.PlayPvs(sound, airlock);
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

    private bool CanChangeState(Entity<AirlockComponent> airlock, bool isPried = false)
    {
        return (isPried || airlock.Comp.Powered) && !DoorSystem.IsBolted(airlock);
    }

}
