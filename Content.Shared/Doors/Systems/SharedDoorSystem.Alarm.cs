using Content.Shared.Doors.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Prying.Components;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{
    private void InitializeAlarm()
    {
        // Access/Prying
        SubscribeLocalEvent<DoorAlarmComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<DoorAlarmComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<DoorAlarmComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<DoorAlarmComponent, GetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
        SubscribeLocalEvent((Entity<DoorAlarmComponent> doorAlarm, ref PriedEvent _) => OnAfterPried(doorAlarm));

        // Visuals
        SubscribeLocalEvent((Entity<DoorAlarmComponent> doorAlarm, ref MapInitEvent _) => UpdateVisuals(doorAlarm));
        SubscribeLocalEvent((Entity<DoorAlarmComponent> doorAlarm, ref ComponentStartup _) => UpdateVisuals(doorAlarm));
    }

    private void OnPowerChanged(Entity<DoorAlarmComponent> doorAlarm, ref PowerChangedEvent args)
    {
        doorAlarm.Comp.IsPowered = args.Powered;
        UpdateVisuals(doorAlarm);

        Dirty(doorAlarm);
    }

    public bool TriggerAlarm(Entity<DoorAlarmComponent> doorAlarm)
    {
        if (doorAlarm.Comp.IsTriggered)
            return true;

        // If the door has a cooldown on being triggered and that cooldown has not expired, do nothing.
        if (doorAlarm.Comp.EmergencyCloseCooldown != null &&
            _gameTiming.CurTime < doorAlarm.Comp.EmergencyCloseCooldown)
            return false;

        doorAlarm.Comp.IsTriggered = true;

        // If this door alarm has a door, try to close it. If it can be legally closed, skip the wait and close it.
        return TryComp<DoorComponent>(doorAlarm, out var door) &&
               TryClose((doorAlarm, door)) &&
               OnPartialClose((doorAlarm, door));
    }

    #region Access/Prying

    private static void OnDoorGetPryTimeModifier(Entity<DoorAlarmComponent> firelock, ref GetPryTimeModifierEvent args)
    {
        if (!firelock.Comp.IsActive)
            return;

        args.PryTimeModifier *= firelock.Comp.LockedPryTimeModifier;
    }

    private void OnBeforePry(Entity<DoorAlarmComponent> doorAlarm, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (!doorAlarm.Comp.IsPowered || args.StrongPry || args.PryPowered)
            return;

        args.Cancelled = true;
    }

    private void OnBeforeDoorOpened(Entity<DoorAlarmComponent> doorAlarm, ref BeforeDoorOpenedEvent args)
    {
        if (!doorAlarm.Comp.IsActive ||
            args.User == null ||
            _accessReaderSystem.IsAllowed(args.User.Value, doorAlarm))
            return;

        args.Cancel();
    }

    private void OnAfterPried(Entity<DoorAlarmComponent> doorAlarm)
    {
        doorAlarm.Comp.EmergencyCloseCooldown = _gameTiming.CurTime + doorAlarm.Comp.EmergencyCloseCooldownDuration;
    }

    #endregion

    #region Visuals

    private void UpdateVisuals(Entity<DoorAlarmComponent> doorAlarm)
    {
        _appearance.SetData(doorAlarm, DoorVisuals.Powered, doorAlarm.Comp.IsPowered);

        if (!TryComp<DoorComponent>(doorAlarm, out var door))
            return;

        _appearance.SetData(
            doorAlarm,
            DoorVisuals.ClosedLights,
            door.State is DoorState.Closed or DoorState.WeldedClosed or DoorState.Denying && doorAlarm.Comp.IsActive
        );
    }

    #endregion
}
