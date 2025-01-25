using Content.Shared.Doors.Components;
using Content.Shared.Power;
using Content.Shared.Prying.Components;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{

    private void InitializeAlarm()
    {
        SubscribeLocalEvent<DoorAlarmComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<DoorAlarmComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<DoorAlarmComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<DoorAlarmComponent, GetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
        SubscribeLocalEvent((Entity<DoorAlarmComponent> doorAlarm, ref PriedEvent args) => OnAfterPried(doorAlarm));
        SubscribeLocalEvent<DoorAlarmComponent, DoorStateChangedEvent>(OnDoorStateChanged);
    }

    private void OnPowerChanged(Entity<DoorAlarmComponent> doorAlarm, ref PowerChangedEvent args)
    {
        doorAlarm.Comp.IsPowered = args.Powered;

        Dirty(doorAlarm);
    }

    public bool TriggerAlarm(Entity<DoorAlarmComponent> doorAlarm, bool predicted = false)
    {
        if (doorAlarm.Comp.IsTriggered)
            return true;

        // If the door has a cooldown on being triggered and that cooldown has not expired, do nothing.
        if (doorAlarm.Comp.EmergencyCloseCooldown != null &&
            _gameTiming.CurTime < doorAlarm.Comp.EmergencyCloseCooldown)
            return false;

        return SetAlarm(doorAlarm, true, predicted);
    }

    protected bool SetAlarm(Entity<DoorAlarmComponent> doorAlarm, bool alarmTriggered, bool predicted = false)
    {
        if (!TryComp<DoorComponent>(doorAlarm, out var door))
            return false;

        var success = alarmTriggered
            ? TryClose((doorAlarm, door), predicted: predicted)
            : TryOpen((doorAlarm, door), predicted: predicted);

        if (!success)
            return false;

        doorAlarm.Comp.IsTriggered = alarmTriggered;

        var ev = new DoorAlarmChangedEvent();
        RaiseLocalEvent(ev);

        Dirty(doorAlarm, doorAlarm.Comp);

        return true;
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

    protected virtual void OnDoorStateChanged(Entity<DoorAlarmComponent> doorAlarm, ref DoorStateChangedEvent args)
    {
        if (args.State != DoorState.Open)
            return;

        // An alarmed door will always de-trigger if the door is opened, allowing the door to re-trigger
        // in the future.
        doorAlarm.Comp.IsTriggered = false;

        Dirty(doorAlarm, doorAlarm.Comp);
    }

    private void OnAfterPried(Entity<DoorAlarmComponent> doorAlarm)
    {
        doorAlarm.Comp.EmergencyCloseCooldown = _gameTiming.CurTime + doorAlarm.Comp.EmergencyCloseCooldownDuration;
    }

    #endregion

}
