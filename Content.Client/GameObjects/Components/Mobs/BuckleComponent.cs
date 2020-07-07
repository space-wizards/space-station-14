using Content.Client.GameObjects.Components.Strap;
using Content.Client.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class BuckleComponent : SharedBuckleComponent, IClientDraggable
    {
        private bool _buckled;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is BuckleComponentState buckle))
            {
                return;
            }

            _buckled = buckle.Buckled;
        }

        protected override bool Buckled => _buckled;

        bool IClientDraggable.ClientCanDropOn(CanDropEventArgs eventArgs)
        {
            return eventArgs.Target.HasComponent<StrapComponent>();
        }

        bool IClientDraggable.ClientCanDrag(CanDragEventArgs eventArgs)
        {
            return true;
        }
    }
}
