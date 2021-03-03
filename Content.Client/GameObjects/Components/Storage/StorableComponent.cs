#nullable enable
using Content.Shared.GameObjects.Components.Storage;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Storage
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStorableComponent))]
    public class StorableComponent : SharedStorableComponent
    {
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not StorableComponentState state)
            {
                return;
            }

            Size = state.Size;
        }
    }
}
