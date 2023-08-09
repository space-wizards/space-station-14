using Content.Shared.Cuffs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Cuffs.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandcuffComponent))]
    public sealed class HandcuffComponent : SharedHandcuffComponent
    {
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not HandcuffedComponentState state)
            {
                return;
            }

            if (state.IconState == string.Empty)
            {
                return;
            }

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<SpriteComponent?>(Owner, out var sprite))
            {
                sprite.LayerSetState(0, new RSI.StateId(state.IconState)); // TODO: safety check to see if RSI contains the state?
            }
        }
    }
}
