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
        if (!_plantTray.TryGetPlant(entity.AsNullable(), out var plant))
            return;

        if (!TryComp<PlantHolderComponent>(plant, out var plantHolder) ||
            !TryComp<PlantComponent>(plant, out var plantComp) ||
            !TryComp<PlantHarvestComponent>(plant, out var harvest))
            return;

        var deviation = plantHolder.Age > plantComp.Maturation
            ? (int)Math.Max(plantComp.Maturation - 1, plantHolder.Age - _random.Next(7, 10))
            : (int)(plantComp.Maturation / plantComp.GrowthStages);

        plantHolder.Age -= deviation;
        plantHolder.SkipAging++;
        entity.Comp.ForceUpdate = true;
        harvest.LastHarvest = plantHolder.Age;
    }
}
