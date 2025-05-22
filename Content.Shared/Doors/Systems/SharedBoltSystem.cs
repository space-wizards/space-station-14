using Content.Shared.Doors.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Prying.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Doors.Systems;

public abstract class SharedBoltSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem  _powerReceiver = default!;
    [Dependency] protected readonly SharedAudioSystem Audio =  default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<DoorBoltComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
        SubscribeLocalEvent<DoorBoltComponent, BeforePryEvent>(OnDoorPry);
        SubscribeLocalEvent<DoorBoltComponent, DoorStateChangedEvent>(OnStateChanged);
    }

    private void OnDoorPry(Entity<DoorBoltComponent> ent, ref BeforePryEvent args)
    {
        if (!args.CanPry)
            return;

        if (!ent.Comp.BoltsDown || args.Strength >= PryStrength.Forced)
            return;

        args.Message = Loc.GetString("pryable-component-cannot-pry-is-bolted-message", ("object", ent.Owner));
        args.CanPry = false;
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

    protected void UpdateBoltLightStatus(Entity<DoorBoltComponent> ent)
    {
        _appearance.SetData(ent, DoorVisuals.BoltLights, GetBoltLightsVisible(ent));
    }

    private static bool GetBoltLightsVisible(Entity<DoorBoltComponent> ent)
    {
        if (ent.Comp is not { BoltLightsEnabled: true, BoltsDown: true })
            return false;

        if (ent.Comp is { BoltsRequirePower: true, Powered: false })
            return false;

        return true;
    }

    public void SetBoltLightsEnabled(Entity<DoorBoltComponent> ent, bool value)
    {
        if (ent.Comp.BoltLightsEnabled == value)
            return;

        ent.Comp.BoltLightsEnabled = value;
        Dirty(ent, ent.Comp);
        UpdateBoltLightStatus(ent);
    }

    public bool TrySetBoltsDown(Entity<DoorBoltComponent> ent, bool value, EntityUid? user = null, bool predicted = false)
    {
        if (ent.Comp.BoltsDown == value)
            return false;

        if (ent.Comp.BoltsRequirePower && !_powerReceiver.IsPowered(ent.Owner))
            return false;

        var args = new BeforeBoltEvent(ent);
        RaiseLocalEvent(ent, ref args);
        if (args.Cancelled)
            return false;

        ent.Comp.BoltsDown = value;
        Dirty(ent, ent.Comp);
        UpdateBoltLightStatus(ent);

        // used to reset the auto-close timer after unbolting
        var ev = new DoorBoltsChangedEvent(value);
        RaiseLocalEvent(ent.Owner, ev);

        var sound = value ? ent.Comp.BoltDownSound : ent.Comp.BoltUpSound;
        if (predicted)
            Audio.PlayPredicted(sound, ent, user: user);
        else
            Audio.PlayPvs(sound, ent);
        return true;
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

    public bool TrySetBoltsToggle(Entity<DoorBoltComponent> ent, EntityUid? user = null, bool predicted = false)
    {
        return TrySetBoltsDown(ent, !ent.Comp.BoltsDown, user, predicted);
    }
}
