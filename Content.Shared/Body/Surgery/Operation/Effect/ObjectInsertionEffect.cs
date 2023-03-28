using Content.Shared.Body.Surgery.Components;

namespace Content.Shared.Body.Surgery.Operation.Effect;

public sealed class ObjectInsertionEffect : IOperationEffect
{
    public void Execute(EntityUid user, OperationComponent operation)
    {
        // TODO: have a container in each limb and add to it?
    }
}
