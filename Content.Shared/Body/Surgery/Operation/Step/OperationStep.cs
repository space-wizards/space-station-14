using Content.Shared.Body.Surgery.Operation.Step.Behavior;
using Content.Shared.Body.Surgery.Operation.Step.Conditional;
using Content.Shared.Body.Surgery.Operation.Step.Insertion;
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

    /// <summary>
    /// Define custom behavior for this step.
    /// The default is to check the tool type and add the step to the operation's tags.
    /// </summary>
    [DataField("behavior")]
    public readonly StepBehavior Behavior = new();

    /// <summary>
    /// Defines what handles inserting items into the patient on this step
    /// </summary>
    [DataField("insertionHandler")]
    public readonly IInsertionHandler? InsertionHandler;

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
