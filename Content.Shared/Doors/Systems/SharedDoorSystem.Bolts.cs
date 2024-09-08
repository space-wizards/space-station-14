using Content.Shared.Doors.Components;
using Content.Shared.Prying.Components;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{
    public void InitializeBolts()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
        SubscribeLocalEvent<DoorBoltComponent, BeforePryEvent>(OnDoorPry);
        SubscribeLocalEvent<DoorBoltComponent, DoorStateChangedEvent>(OnStateChanged);
    }

    private void OnDoorPry(EntityUid uid, DoorBoltComponent component, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (!component.BoltsDown || args.Force)
            return;

        args.Message = "airlock-component-cannot-pry-is-bolted-message";

        args.Cancelled = true;
    }

    private void OnBeforeDoorOpened(EntityUid uid, DoorBoltComponent component, BeforeDoorOpenedEvent args)
    {
        if (component.BoltsDown)
            args.Cancel();
    }

    private void OnBeforeDoorClosed(EntityUid uid, DoorBoltComponent component, BeforeDoorClosedEvent args)
    {
        if (component.BoltsDown)
            args.Cancel();
    }

    private void OnBeforeDoorDenied(EntityUid uid, DoorBoltComponent component, BeforeDoorDeniedEvent args)
    {
        if (component.BoltsDown)
            args.Cancel();
    }

    public void SetBoltWireCut(Entity<DoorBoltComponent> ent, bool value)
    {
        ent.Comp.BoltWireCut = value;
        Dirty(ent, ent.Comp);
    }

    public void UpdateBoltLightStatus(Entity<DoorBoltComponent> ent)
    {
        AppearanceSystem.SetData(ent, DoorVisuals.BoltLights, GetBoltLightsVisible(ent));
    }

    public bool GetBoltLightsVisible(Entity<DoorBoltComponent> ent)
    {
        return ent.Comp.BoltLightsEnabled &&
               ent.Comp.BoltsDown &&
               ent.Comp.Powered;
    }

    public void SetBoltLightsEnabled(Entity<DoorBoltComponent> ent, bool value)
    {
        if (ent.Comp.BoltLightsEnabled == value)
            return;

        ent.Comp.BoltLightsEnabled = value;
        Dirty(ent, ent.Comp);
        UpdateBoltLightStatus(ent);
    }

    public void SetBoltsDown(Entity<DoorBoltComponent> ent, bool value, EntityUid? user = null, bool predicted = false)
    {
        if (ent.Comp.BoltsDown == value)
            return;

        ent.Comp.BoltsDown = value;
        Dirty(ent, ent.Comp);
        UpdateBoltLightStatus(ent);

        var sound = value ? ent.Comp.BoltDownSound : ent.Comp.BoltUpSound;
        if (predicted)
            Audio.PlayPredicted(sound, ent, user: user);
        else
            Audio.PlayPvs(sound, ent);
    }

    private void OnStateChanged(Entity<DoorBoltComponent> entity, ref DoorStateChangedEvent args)
    {
        // If the door is closed, we should look if the bolt was locked while closing
        UpdateBoltLightStatus(entity);
    }

    public bool IsBolted(EntityUid uid, DoorBoltComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return false;
        }

        return component.BoltsDown;
    }
}
