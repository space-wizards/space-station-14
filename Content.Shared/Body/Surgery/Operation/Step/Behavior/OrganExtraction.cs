using Content.Shared.Body.Surgery.Systems;

namespace Content.Shared.Body.Surgery.Operation.Step.Behavior;

public sealed class OrganExtraction : IStepBehavior
{
    public bool CanPerform(SurgeryStepContext context)
    {
        return context.Operation.SelectedOrgan != null;
    }

    public bool Perform(SurgeryStepContext context)
    {
        var organ = context.Operation.SelectedOrgan.Value;
        return SurgerySystem.RemoveOrgan(context.Surgeon, organ);
    }
}
