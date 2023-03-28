using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Operation.Step.Behavior;

[ImplicitDataDefinitionForInheritors]
public interface IStepBehavior
{
    public bool CanPerform(SurgeryStepContext context);

    public bool Perform(SurgeryStepContext context);

    public void OnPerformDelayBegin(SurgeryStepContext context) { }

    public void OnPerformSuccess(SurgeryStepContext context) { }

    public void OnPerformFail(SurgeryStepContext context) { }
}
