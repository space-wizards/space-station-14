using Content.Shared.Body.Surgery.Systems;

namespace Content.Shared.Body.Surgery.Operation.Step.Behavior;

public sealed class OrganExtraction : StepBehavior
{
    public override bool CanPerform(SurgeryStepContext context)
    {
        // need to select organ in the ui first
        return base.CanPerform(context) && context.Operation.SelectedOrgan != null;
    }

    public override bool Perform(SurgeryStepContext context)
    {
        var organ = context.Operation.SelectedOrgan!.Value;
        if (!context.SurgerySystem.RemoveOrgan(context.Surgeon, organ))
            return false;

        return base.Perform(context);
    }

    public override void OnPerformFail(SurgeryStepContext context)
    {
        if (context.Operation.SelectedOrgan == null)
        {
            context.OperationSystem.Popup(Loc.GetString("surgery-step-no-organ-selected"), context.Surgeon);
        }
        else
        {
            base.OnPerformFail(context);
        }
    }
}
