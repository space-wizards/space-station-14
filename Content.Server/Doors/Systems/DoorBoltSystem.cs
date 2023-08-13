using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;

namespace Content.Server.Doors.Systems;

public sealed class DoorBoltSystem : SharedDoorBoltSystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorBoltComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<DoorBoltComponent, DoorStateChangedEvent>(OnStateChanged);
    }

    private void OnPowerChanged(EntityUid uid, DoorBoltComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            if (component.BoltWireCut)
                SetBoltsWithAudio(uid, component, true);
        }

        UpdateBoltLightStatus(uid, component);
    }

    public void UpdateBoltLightStatus(EntityUid uid, DoorBoltComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        Appearance.SetData(uid, DoorVisuals.BoltLights, GetBoltLightsVisible(uid, component), appearance);
    }

    public bool GetBoltLightsVisible(EntityUid uid, DoorBoltComponent component)
    {
        return component.BoltLightsEnabled &&
               component.BoltsDown &&
               this.IsPowered(uid, EntityManager);
    }

    public void SetBoltLightsEnabled(EntityUid uid, DoorBoltComponent component, bool value)
    {
        if (component.BoltLightsEnabled == value)
            return;

        component.BoltLightsEnabled = value;
        UpdateBoltLightStatus(uid, component);
    }

    public void SetBoltsDown(EntityUid uid, DoorBoltComponent component, bool value)
    {
        if (component.BoltsDown == value)
            return;

        component.BoltsDown = value;
        UpdateBoltLightStatus(uid, component);
    }

    private void OnStateChanged(EntityUid uid, DoorBoltComponent component, DoorStateChangedEvent args)
    {
        // If the door is closed, we should look if the bolt was locked while closing
        UpdateBoltLightStatus(uid, component);
    }

    public void SetBoltsWithAudio(EntityUid uid, DoorBoltComponent component, bool newBolts)
    {
        if (newBolts == component.BoltsDown)
            return;

        component.BoltsDown = newBolts;
        Audio.PlayPvs(newBolts ? component.BoltDownSound : component.BoltUpSound, uid);
        UpdateBoltLightStatus(uid, component);
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

