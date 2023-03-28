using Content.Shared.Body.Surgery.Components;

namespace Content.Shared.Body.Surgery.Operation.Effect;

public sealed class BoneRepairEffect : IOperationEffect
{
    public void Execute(EntityUid user, OperationComponent operation)
    {
        // TODO: repair broken hairline/compound fractures
    }
}
