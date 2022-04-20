using Content.Shared.Climbing;

namespace Content.Client.Movement.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedClimbingComponent))]
public sealed class ClimbingComponent : SharedClimbingComponent
{
    public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
    {
        base.HandleComponentState(curState, nextState);

        if (curState is not ClimbModeComponentState climbModeState)
        {
            return;
        }

        IsClimbing = climbModeState.Climbing;
        OwnerIsTransitioning = climbModeState.IsTransitioning;
    }
}
