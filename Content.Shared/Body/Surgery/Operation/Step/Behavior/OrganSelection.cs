using Content.Shared.Body.Surgery.Systems;

namespace Content.Shared.Body.Surgery.Operation.Step.Behavior;

public sealed class OrganSelection : IStepBehavior
{
    public bool CanPerform(SurgeryStepContext context)
    {
        return context.Operation.SelectedOrgan == null;
    }

    public bool Perform(SurgeryStepContext context)
    {
        context.SurgerySystem.SelectOrgan(context.Operation.Part, context.Surgeon, context.Target);
        return true;
    }
}
