using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Plant mutation entity effect that changes repeatability of plant harvesting (without re-planting).
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateHarvestEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateHarvest>
{
    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateHarvest> args)
    {
        var harvest = EnsureComp<PlantHarvestComponent>(entity.Owner);
        switch (harvest.HarvestRepeat)
        {
            case HarvestType.NoRepeat:
                harvest.HarvestRepeat = HarvestType.Repeat;
                break;
            case HarvestType.Repeat:
                harvest.HarvestRepeat = HarvestType.SelfHarvest;
                break;
        }
    }
}
