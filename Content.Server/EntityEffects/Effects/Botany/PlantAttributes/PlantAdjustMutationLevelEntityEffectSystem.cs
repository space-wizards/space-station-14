using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAdjustMutationLevelEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantAdjustMutationLevel>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantAdjustMutationLevel> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        entity.Comp.MutationLevel += args.Effect.Amount * entity.Comp.MutationMod;
        _plantHolder.CheckHealth(entity, entity.Comp);
    }
}
