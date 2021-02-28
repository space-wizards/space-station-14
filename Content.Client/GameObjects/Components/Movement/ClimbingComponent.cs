using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedClimbingComponent))]
    public class ClimbingComponent : SharedClimbingComponent
    {
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
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
}
