using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Systems;

public sealed class MovementIgnoreGravitySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MovementIgnoreGravityComponent, ComponentGetState>(GetState);
        SubscribeLocalEvent<MovementIgnoreGravityComponent, ComponentHandleState>(HandleState);
        SubscribeLocalEvent<MovementAlwaysTouchingComponent, CanWeightlessMoveEvent>(OnWeightless);
    }

    private void OnWeightless(EntityUid uid, MovementAlwaysTouchingComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
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
