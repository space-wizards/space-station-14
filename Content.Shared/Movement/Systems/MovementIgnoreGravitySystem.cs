using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Systems;

public sealed class MovementIgnoreGravitySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MovementIgnoreGravityComponent, ComponentGetState>(GetState);
        SubscribeLocalEvent<MovementIgnoreGravityComponent, ComponentHandleState>(HandleState);
    }

    private void HandleState(EntityUid uid, MovementIgnoreGravityComponent component, ref ComponentHandleState args)
    {
        if (args.Next is null)
            return;

        component.Weightless = ((MovementIgnoreGravityComponentState) args.Next).Weightless;
    }

    private void GetState(EntityUid uid, MovementIgnoreGravityComponent component, ref ComponentGetState args)
    {
        args.State = new MovementIgnoreGravityComponentState(component);
    }
}
