using Content.Server.Administration.Verbs.Operations;
using Content.Server.Administration.Verbs.Operations.Smites;
using Content.Shared.Body.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSpillBloodstream(Entity<BloodstreamComponent> entity, ref AdminOperationEvent<SpillBloodstreamOperation> args)
    {
        _bloodstream.SpillAllSolutions(entity.AsNullable());
    }
}
