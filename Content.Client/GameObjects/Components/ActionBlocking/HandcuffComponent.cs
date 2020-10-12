using Content.Shared.GameObjects.Components.ActionBlocking;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class HandcuffComponent : SharedHandcuffComponent
    {
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            var cuffState = curState as HandcuffedComponentState;

            if (cuffState == null || cuffState.IconState == string.Empty)
            {
                return;
            }

            if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
            {
                sprite.LayerSetState(0, new RSI.StateId(cuffState.IconState)); // TODO: safety check to see if RSI contains the state?
            }
        }
    }
}
