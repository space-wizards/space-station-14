using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that reverts aging of plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantCryoxadoneEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantCryoxadone>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantCryoxadone> args)
    {
        if (!_plantTray.HasPlant(entity.AsNullable()))
            return;

        var plantUid = entity.Comp.PlantEntity!.Value;
        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder) ||
            !TryComp<PlantComponent>(plantUid, out var plant) ||
            !TryComp<PlantHarvestComponent>(plantUid, out var harvest))
            return;

        var deviation = plantHolder.Age > plant.Maturation
            ? (int)Math.Max(plant.Maturation - 1, plantHolder.Age - _random.Next(7, 10))
            : (int)(plant.Maturation / plant.GrowthStages);

        plantHolder.Age -= deviation;
        plantHolder.SkipAging++;
        entity.Comp.ForceUpdate = true;
        harvest.LastHarvest = plantHolder.Age;
    }
}
