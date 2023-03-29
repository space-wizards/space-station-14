using Content.Shared.Body.Surgery.Systems;

namespace Content.Shared.Body.Surgery.Operation.Step.Behavior;

public sealed class OrganSelection : StepBehavior
{
    public override bool CanPerform(SurgeryStepContext context)
    {
        return base.CanPerform(context) && context.Operation.SelectedOrgan == null;
    }

    public override bool Perform(SurgeryStepContext context)
    {
        context.SurgerySystem.SelectOrgan(context.Operation.Part, context.Surgeon, context.Target);
        return base.Perform(context);
    }
}
