
using Robust.Shared.GameObjects;
using Content.Shared.GameObjects.Components.ActionBlocking;
using Content.Client.GameObjects.Components.Items;

namespace Content.Client.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class CuffedComponent : SharedCuffedComponent
    {
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            var cuffState = curState as CuffedComponentState;

            if (cuffState == null)
            {
                return;
            }

            CanStillInteract = cuffState.CanStillInteract;

            if (Owner.TryGetComponent<HandsComponent>(out var hands))
            {
                // do other logic?
            }
        }
    }
}
