using Content.Shared.Doors.Components;
using Content.Shared.Popups;

namespace Content.Shared.Doors.Systems;

public abstract class SharedAirlockSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedDoorSystem DoorSystem = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
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
}
