using Content.Shared.Body.Surgery.Components;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Operation.Step.Conditional;

[ImplicitDataDefinitionForInheritors]
public interface IOperationStepConditional
{
    bool Necessary(OperationComponent operation);
}
