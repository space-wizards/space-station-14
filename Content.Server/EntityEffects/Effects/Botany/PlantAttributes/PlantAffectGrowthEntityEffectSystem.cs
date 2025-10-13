using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAffectGrowthEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantAffectGrowth>
{
    [Dependency] private readonly BasicGrowthSystem _plantGrowth = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantAffectGrowth> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        _plantGrowth.AffectGrowth(entity, (int)args.Effect.Amount, entity.Comp);
    }
}
