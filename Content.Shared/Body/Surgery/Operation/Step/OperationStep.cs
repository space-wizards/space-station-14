using Content.Shared.Body.Surgery.Operation.Step.Behavior;
using Content.Shared.Body.Surgery.Operation.Step.Conditional;
using Content.Shared.Body.Surgery.Components;
using Robust.Shared.Prototypes;
//using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Operation.Step;

[DataDefinition]
public sealed class OperationStep
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField("conditional")]
    public readonly IOperationStepConditional? Conditional;

    [DataField("behavior", serverOnly: true)]
    public readonly IStepBehavior? Behavior = new AddTag();

    public bool Necessary(OperationComponent operation)
    {
        return Conditional?.Necessary(operation) ?? true;
    }

    public bool CanPerform(SurgeryStepContext context)
    {
        return Behavior?.CanPerform(context) ?? false;
    }

    public bool Perform(SurgeryStepContext context)
    {
        return Behavior?.Perform(context) ?? false;
    }

    public void OnPerformDelayBegin(SurgeryStepContext context)
    {
        Behavior?.OnPerformDelayBegin(context);
    }

    public void OnPerformSuccess(SurgeryStepContext context)
    {
        Behavior?.OnPerformSuccess(context);
    }

    public void OnPerformFail(SurgeryStepContext context)
    {
        Behavior?.OnPerformFail(context);
    }
}
