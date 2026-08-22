using Content.Server.Administration.Verbs.Operations;
using Content.Server.Administration.Verbs.Operations.Smites;
using Content.Shared.Slippery;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSuperSlip(Entity<MetaDataComponent> entity, ref AdminOperationEvent<SuperSlipOperation> args)
    {
        var hadSlipComponent = EnsureComp(entity, out SlipperyComponent slipComponent);
        if (!hadSlipComponent)
        {
            slipComponent.SlipData.SuperSlippery = true;
            slipComponent.SlipData.StunTime = TimeSpan.FromSeconds(5);
            slipComponent.SlipData.LaunchForwardsMultiplier = 20;
        }

        _slippery.TrySlip(entity, slipComponent, entity, false);
        if (!hadSlipComponent)
            RemComp(entity, slipComponent);
    }
}
