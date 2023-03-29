using Content.Shared.Body.Surgery.Components;

namespace Content.Shared.Body.Surgery.Operation.Effect;

public sealed class CompoundRepairEffect : IOperationEffect
{
    public void Execute(EntityUid user, OperationComponent operation)
    {
        // TODO: remove any compound fracture wounds on part
    }
}
