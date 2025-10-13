using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantCryoxadoneEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantCryoxadone>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantCryoxadone> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        var deviation = 0;
        var seed = entity.Comp.Seed;
        if (seed == null)
            return;
        if (entity.Comp.Age > seed.Maturation)
            deviation = (int) Math.Max(seed.Maturation - 1, entity.Comp.Age - _random.Next(7, 10));
        else
            deviation = (int) (seed.Maturation / seed.GrowthStages);
        entity.Comp.Age -= deviation;
        entity.Comp.LastProduce = entity.Comp.Age;
        entity.Comp.SkipAging++;
        entity.Comp.ForceUpdate = true;
    }
}
