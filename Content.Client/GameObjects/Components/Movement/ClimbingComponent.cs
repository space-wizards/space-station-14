using Content.Client.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class ClimbingComponent : SharedClimbingComponent, IClientDraggable
    {
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is ClimbModeComponentState climbModeState) || Body == null)
            {
                return;
            }

            IsClimbing = climbModeState.Climbing; 
        }

        public override bool IsClimbing { get; set; }

        bool IClientDraggable.ClientCanDropOn(CanDropEventArgs eventArgs)
        {
            return eventArgs.Target.HasComponent<IClimbable>();
        }

        bool IClientDraggable.ClientCanDrag(CanDragEventArgs eventArgs)
        {
            return true;
        }
    }
}
