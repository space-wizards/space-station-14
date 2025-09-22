using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.NewEffects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAdjustMutationLevelEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, Shared.EntityEffects.Effects.Botany.PlantAttributes.PlantAdjustMutationLevel>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<Shared.EntityEffects.Effects.Botany.PlantAttributes.PlantAdjustMutationLevel> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        entity.Comp.Health += args.Effect.Amount;
        _plantHolder.CheckHealth(entity, entity.Comp);
    }
}
