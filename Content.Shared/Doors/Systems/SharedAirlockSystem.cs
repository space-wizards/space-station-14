using Content.Shared.Doors.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Systems;

public abstract class SharedAirlockSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedDoorSystem DoorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedAirlockComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<SharedAirlockComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<SharedAirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
    }

    private void OnGetState(EntityUid uid, SharedAirlockComponent airlock, ref ComponentGetState args)
    {
        // Need to network airlock safety state to avoid mis-predicts when a door auto-closes as the client walks through the door.
        args.State = new AirlockComponentState(airlock.Safety);
    }

    private void OnHandleState(EntityUid uid, SharedAirlockComponent airlock, ref ComponentHandleState args)
    {
        if (args.Current is not AirlockComponentState state)
            return;

        airlock.Safety = state.Safety;
    }

    protected virtual void OnBeforeDoorClosed(EntityUid uid, SharedAirlockComponent airlock, BeforeDoorClosedEvent args)
    {
        if (!airlock.Safety)
            args.PerformCollisionCheck = false;
    }


    public void UpdateEmergencyLightStatus(SharedAirlockComponent component)
    {
        Appearance.SetData(component.Owner, DoorVisuals.EmergencyLights, component.EmergencyAccess);
    }

    public void ToggleEmergencyAccess(SharedAirlockComponent component)
    {
        component.EmergencyAccess = !component.EmergencyAccess;
        UpdateEmergencyLightStatus(component);
    }
}
