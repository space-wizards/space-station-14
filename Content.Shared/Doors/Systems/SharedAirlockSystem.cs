using Content.Shared.Doors.Components;
using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Systems;

public abstract class SharedAirlockSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedDoorSystem DoorSystem = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<AirlockComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
    }

    private void OnGetState(EntityUid uid, AirlockComponent airlock, ref ComponentGetState args)
    {
        // Need to network airlock safety state to avoid mis-predicts when a door auto-closes as the client walks through the door.
        args.State = new AirlockComponentState(airlock.Safety);
    }

    private void OnHandleState(EntityUid uid, AirlockComponent airlock, ref ComponentHandleState args)
    {
        if (args.Current is not AirlockComponentState state)
            return;

        airlock.Safety = state.Safety;
    }

    protected virtual void OnBeforeDoorClosed(EntityUid uid, AirlockComponent airlock, BeforeDoorClosedEvent args)
    {
        if (!airlock.Safety)
            args.PerformCollisionCheck = false;
    }


    public void UpdateEmergencyLightStatus(EntityUid uid, AirlockComponent component)
    {
        Appearance.SetData(uid, DoorVisuals.EmergencyLights, component.EmergencyAccess);
    }

    public void ToggleEmergencyAccess(EntityUid uid, AirlockComponent component)
    {
        component.EmergencyAccess = !component.EmergencyAccess;
        UpdateEmergencyLightStatus(uid, component);
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

    public void SetBoltWireCut(AirlockComponent component, bool value)
    {
        component.BoltWireCut = value;
    }
}
