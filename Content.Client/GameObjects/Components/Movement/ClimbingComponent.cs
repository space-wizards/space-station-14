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

            if (curState is not ClimbModeComponentState climbModeState || Body == null)
            {
                return;
            }

            IsClimbing = climbModeState.Climbing;
        }

        public override bool IsClimbing { get; set; }
    }
}
