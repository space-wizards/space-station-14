namespace Content.Shared.Body.Surgery.Operation.Step.Behavior;

public sealed class AddTag : IStepBehavior
{
    public bool CanPerform(SurgeryStepContext context)
    {
        return context.Tool.Steps.Contains(context.Step.ID) && context.OperationSystem.CanAddSurgeryTag(context.Operation, context.Step.ID);
    }

    public bool Perform(SurgeryStepContext context)
    {
        context.OperationSystem.AddSurgeryTag(context.Surgeon, context.Operation, context.Step.ID);
        return true;
    }

    public void OnPerformDelayBegin(SurgeryStepContext context)
    {
        context.OperationSystem.DoBeginPopups(context.Surgeon, context.Target, context.Operation.Part, context.Step.ID);
    }

    public void OnPerformSuccess(SurgeryStepContext context)
    {
        context.OperationSystem.DoSuccessPopups(context.Surgeon, context.Target, context.Operation.Part, context.Step.ID);
    }

    public void OnPerformFail(SurgeryStepContext context)
    {
        context.OperationSystem.DoFailurePopup(context.Surgeon);
    }
}
