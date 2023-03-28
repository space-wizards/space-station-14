using Content.Shared.Body.Surgery.Systems;

namespace Content.Shared.Body.Surgery.Operation.Step.Behavior;

public sealed class OrganSelection : IStepBehavior
{
    // FIXME change to context field because injection brokey
    [Dependency] private readonly SharedSurgerySystem _surgery = default!;

    public bool CanPerform(SurgeryStepContext context)
    {
        return context.Operation.SelectedOrgan == null;
    }

    public bool Perform(SurgeryStepContext context)
    {
        _surgery.SelectOrgan(context.Surgeon, context.Target);
        return true;
    }
}
