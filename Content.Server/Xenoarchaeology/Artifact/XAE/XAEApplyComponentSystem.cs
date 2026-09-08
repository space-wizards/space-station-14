using Content.Server.Power.Components;
using Content.Server.Spawners.Components;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.EntityTable.ValueSelector;
using Content.Shared.Random.Helpers;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;
using Robust.Shared.Timing;

/// <inheritdoc />
public sealed class XAEApplyComponentSystem : SharedXAEApplyComponentsSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    protected override bool TryApplyModifiers(IComponent component, XenoArtifactEffectsModifications modifications, EntityUid artifact)
    {
        return component switch
        {
            PowerSupplierComponent powerSupplier => TryApplyModifiersFor(powerSupplier, modifications),
            EntityTableSpawnerComponent spawner => TryApplyModifiersFor(spawner, modifications, artifact),
            _ => base.TryApplyModifiers(component, modifications, artifact)
        };
    }

    private bool TryApplyModifiersFor(EntityTableSpawnerComponent tableSpawner, XenoArtifactEffectsModifications modifications, EntityUid owner)
    {
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier) && tableSpawner.Table is EntSelector entSelector)
        {
            var pr = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(owner));
            var originalAmount = entSelector.Amount.Get(pr);
            var newAmount = modifier.Modify(originalAmount);
            entSelector.Amount = new ConstantNumberSelector((int)newAmount);
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(PowerSupplierComponent powerSupplier, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier))
        {
            powerSupplier.MaxSupply = modifier.Modify(powerSupplier.MaxSupply);
            return true;
        }

        return false;
    }
}
