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
        if (!TryComp<PlantTraitsComponent>(entity, out var traits))
            return;

        if (entity.Comp.Age > traits.Maturation)
            deviation = (int)Math.Max(traits.Maturation - 1, entity.Comp.Age - _random.Next(7, 10));
        else
            deviation = (int)(traits.Maturation / traits.GrowthStages);
        entity.Comp.Age -= deviation;
        entity.Comp.LastProduce = entity.Comp.Age;
        entity.Comp.SkipAging++;
        entity.Comp.ForceUpdate = true;
    }
}
