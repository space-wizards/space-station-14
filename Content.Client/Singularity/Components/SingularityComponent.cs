using Content.Shared.Singularity.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Singularity.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSingularityComponent))]
    public sealed class SingularityComponent : SharedSingularityComponent
    {
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not SingularityComponentState state)
                return;

            EntitySystem.Get<SharedSingularitySystem>().SetSingularityLevel(this, state.Level);
        }
    }
}
