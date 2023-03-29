using Content.Shared.Body.Surgery.Components;
using Content.Shared.Body.Surgery.Systems;

namespace Content.Shared.Body.Surgery.Operation.Step;

public sealed class SurgeryStepContext
{
    /// <summary>
    /// The entity being operated on as a whole, might be the same as Operation.Part.
    /// </summary>
    public readonly EntityUid Target;

    /// <summary>
    /// Entity that is doing the operation.
    /// </summary>
    public readonly EntityUid Surgeon;

    /// <summary>
    /// The operation, belongs to target
    /// </summary>
    public readonly OperationComponent Operation;

    /// <summary>
    /// Tool being used to complete this step
    /// </summary>
    public readonly SurgeryToolComponent Tool;

    /// <summary>
    /// Step to be completed.
    /// </summary>
    public readonly OperationStep Step;

    /// <summary>
    /// Reference to the operation system
    /// </summary>
    public readonly OperationSystem OperationSystem;

    /// <summary>
    /// Reference to the surgery system
    /// </summary>
    public readonly SharedSurgerySystem SurgerySystem;

    public SurgeryStepContext(
        EntityUid target,
        EntityUid surgeon,
        OperationComponent operation,
        SurgeryToolComponent tool,
        OperationStep step,
        OperationSystem operationSystem,
        SharedSurgerySystem surgerySystem)
    {
        Target = target;
        Surgeon = surgeon;
        Operation = operation;
        Tool = tool;
        Step = step;
        OperationSystem = operationSystem;
        SurgerySystem = surgerySystem;
    }
}
