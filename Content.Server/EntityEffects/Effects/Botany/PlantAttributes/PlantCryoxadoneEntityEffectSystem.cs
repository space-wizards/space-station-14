using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that reverts aging of plant.
/// </summary>
public sealed partial class PlantCryoxadoneEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantCryoxadone>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantCryoxadone> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        if (!TryComp<PlantTraitsComponent>(entity, out var traits) || !TryComp<PlantHarvestComponent>(entity, out var harvest))
            return;

        var deviation = entity.Comp.Age > traits.Maturation
            ? (int)Math.Max(traits.Maturation - 1, entity.Comp.Age - _random.Next(7, 10))
            : (int)(traits.Maturation / traits.GrowthStages);

        entity.Comp.Age -= deviation;
        entity.Comp.SkipAging++;
        entity.Comp.ForceUpdate = true;
        harvest.LastHarvest = entity.Comp.Age;
    }
}
