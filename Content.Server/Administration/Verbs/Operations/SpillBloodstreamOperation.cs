using Content.Shared.Body.Components;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSpillBloodstream(Entity<BloodstreamComponent> entity, ref AdminOperationEvent<SpillBloodstreamOperation> args)
    {
        _bloodstream.SpillAllSolutions(entity.AsNullable());
    }
}

// TODO: Use EntityEffectsOperation once spilling bloodstream solutions has an entity effect.
public sealed partial class SpillBloodstreamOperation : AdminOperationBase<SpillBloodstreamOperation>;
