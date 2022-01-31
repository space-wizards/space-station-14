using Content.Shared.Doors.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using System.Linq;

namespace Content.Shared.Doors.Systems;

public abstract class SharedAirlockSystem : EntitySystem
{
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
        if (airlock.Safety && DoorSystem.GetColliding(uid).Any())
            args.Cancel();
    }
}
