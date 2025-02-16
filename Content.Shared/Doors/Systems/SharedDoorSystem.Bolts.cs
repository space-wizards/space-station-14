using Content.Shared.Doors.Components;
using Content.Shared.Prying.Components;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{

    public void InitializeBolts()
    {
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
        SubscribeLocalEvent<DoorBoltComponent, BeforePryEvent>(OnDoorPry);
        SubscribeLocalEvent<DoorBoltComponent, DoorStateChangedEvent>(OnStateChanged);
    }

    private static void OnDoorPry(Entity<DoorBoltComponent> door, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (!door.Comp.BoltsDown || args.Force)
            return;

        args.Message = "airlock-component-cannot-pry-is-bolted-message";

        args.Cancelled = true;
    }

    private static void OnBeforeDoorOpened(Entity<DoorBoltComponent> door, ref BeforeDoorOpenedEvent args)
    {
        if (door.Comp.BoltsDown)
            args.Cancel();
    }

    private static void OnBeforeDoorClosed(Entity<DoorBoltComponent> door, ref BeforeDoorClosedEvent args)
    {
        if (door.Comp.BoltsDown)
            args.Cancel();
    }

    private static void OnBeforeDoorDenied(Entity<DoorBoltComponent> door, ref BeforeDoorDeniedEvent args)
    {
        if (door.Comp.BoltsDown)
            args.Cancel();
    }

    public void SetBoltWireCut(Entity<DoorBoltComponent> door, bool value)
    {
        door.Comp.BoltWireCut = value;

        Dirty(door, door.Comp);
    }

    protected void UpdateBoltLightStatus(Entity<DoorBoltComponent> door)
    {
        _appearance.SetData(door, DoorVisuals.BoltLights, GetBoltLightsVisible(door));
    }

    public static bool GetBoltLightsVisible(Entity<DoorBoltComponent> door)
    {
        return door.Comp is { BoltLightsEnabled: true, BoltsDown: true, Powered: true };
    }

    public void SetBoltLightsEnabled(Entity<DoorBoltComponent> door, bool value)
    {
        if (door.Comp.BoltLightsEnabled == value)
            return;

        door.Comp.BoltLightsEnabled = value;

        Dirty(door, door.Comp);

        UpdateBoltLightStatus(door);
    }

    public void SetBoltsDown(Entity<DoorBoltComponent> door, bool value, EntityUid? user = null, bool predicted = false)
    {
        TrySetBoltDown(door, value, user, predicted);
    }

    public bool TrySetBoltDown(Entity<DoorBoltComponent> door,
        bool value,
        EntityUid? user = null,
        bool predicted = false)
    {
        if (!_powerReceiver.IsPowered(door.Owner))
            return false;

        if (door.Comp.BoltsDown == value)
            return false;

        door.Comp.BoltsDown = value;

        Dirty(door, door.Comp);
        UpdateBoltLightStatus(door);

        // Used to reset the auto-close timer after unbolting.
        var ev = new DoorBoltsChangedEvent(value);
        RaiseLocalEvent(door.Owner, ev);

        var sound = value ? door.Comp.BoltDownSound : door.Comp.BoltUpSound;
        if (predicted)
            _audio.PlayPredicted(sound, door, user: user);
        else
            _audio.PlayPvs(sound, door);

        return true;
    }

    private void OnStateChanged(Entity<DoorBoltComponent> entity, ref DoorStateChangedEvent args)
    {
        // If the door is closed, we should look if the bolt was locked while closing
        UpdateBoltLightStatus(entity);
    }

    public bool IsBolted(EntityUid uid, DoorBoltComponent? component = null)
    {
        return Resolve(uid, ref component) && component.BoltsDown;
    }
}
