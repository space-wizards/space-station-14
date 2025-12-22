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
public sealed partial class PlantCryoxadoneEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantCryoxadone>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantCryoxadone> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        if (!TryComp<PlantHolderComponent>(entity, out var plantHolder)
            || !TryComp<PlantComponent>(entity, out var plantComp)
            || !TryComp<PlantHarvestComponent>(entity, out var harvest))
            return;

        var deviation = plantHolder.Age > plantComp.Maturation
            ? (int)Math.Max(plantComp.Maturation - 1, plantHolder.Age - _random.Next(7, 10))
            : (int)(plantComp.Maturation / plantComp.GrowthStages);

        plantHolder.Age -= deviation;
        plantHolder.SkipAging++;
        plantHolder.ForceUpdate = true;
        harvest.LastHarvest = plantHolder.Age;
    }
}
