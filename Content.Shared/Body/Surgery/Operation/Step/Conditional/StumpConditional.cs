//using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery.Components;

namespace Content.Shared.Body.Surgery.Operation.Step.Conditional;

public sealed class StumpConditional : IOperationStepConditional
{
    public bool Necessary(OperationComponent operation)
    {
        // TODO: check for stump
        return false;
    }
}
