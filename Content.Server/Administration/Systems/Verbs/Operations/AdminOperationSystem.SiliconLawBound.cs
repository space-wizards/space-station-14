using Content.Server.Administration.Verbs.Operations;
using Content.Server.Administration.Verbs.Operations.Smites;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSiliconLawBound(Entity<SiliconLawProviderComponent> entity, ref AdminOperationEvent<SiliconLawBoundOperation> args)
    {
        EnsureComp<SiliconLawBoundComponent>(entity);

        // The provider was configured by an earlier operation; resolve its laws before notifying the target.
        _siliconLaws.GetLaws(entity.Owner);
        _siliconLaws.NotifyLawsChanged(entity);
    }
}
