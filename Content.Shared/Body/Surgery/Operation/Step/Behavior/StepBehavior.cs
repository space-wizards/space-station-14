namespace Content.Shared.Body.Surgery.Operation.Step.Behavior;

/// <summary>
/// Handles required step tools and progressing the surgery.
/// </summary>
[Virtual]
public class StepBehavior
{
    public virtual bool CanPerform(SurgeryStepContext context)
    {
        return context.Tool.Steps.Contains(context.Step) && context.OperationSystem.CanAddSurgeryTag(context.Operation, context.Step);
    }

    public virtual bool Perform(SurgeryStepContext context)
    {
        context.OperationSystem.AddSurgeryTag(context.Surgeon, context.Target, context.Operation, context.Step);
        return true;
    }

    public virtual void OnPerformDelayBegin(SurgeryStepContext context)
    {
        context.OperationSystem.DoBeginPopups(context.Surgeon, context.Target, context.Operation.Part, context.Step);
    }

    public virtual void OnPerformSuccess(SurgeryStepContext context)
    {
        context.OperationSystem.DoSuccessPopups(context.Surgeon, context.Target, context.Operation.Part, context.Step);
    }

    public virtual void OnPerformFail(SurgeryStepContext context)
    {
        context.OperationSystem.Popup(Loc.GetString("surgery-step-not-useful"), context.Surgeon);
    }
}
