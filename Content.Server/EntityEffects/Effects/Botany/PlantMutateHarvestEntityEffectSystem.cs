using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Plant mutation entity effect that changes repeatability of plant harvesting (without re-planting).
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateHarvestEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantMutateHarvest>
{
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantMutateHarvest> args)
    {
        if (!_plantTray.TryGetPlant(entity.AsNullable(), out var plant))
            return;

        var harvest = EnsureComp<PlantHarvestComponent>(plant.Value);
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
