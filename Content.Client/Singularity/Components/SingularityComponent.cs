using Content.Shared.Singularity.Components;
using Content.Client.Singularity.EntitySystems;

namespace Content.Client.Singularity.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedSingularityComponent))]
public sealed class SingularityComponent : SharedSingularityComponent
{
    public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
    {
        if (curState is not SingularityComponentState state)
            return;

        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SingularitySystem>().SetSingularityLevel(this, state.Level);
    }
}
