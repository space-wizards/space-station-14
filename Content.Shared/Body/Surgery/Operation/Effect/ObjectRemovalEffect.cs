using Content.Shared.Body.Surgery.Components;

namespace Content.Shared.Body.Surgery.Operation.Effect;

public sealed class ObjectRemovalEffect : IOperationEffect
{
    public void Execute(EntityUid user, OperationComponent operation)
    {
        // TODO: have a container in each limb and remove from it
    }
}
