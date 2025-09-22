using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.NewEffects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAdjustMutationModEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, Shared.EntityEffects.Effects.Botany.PlantAttributes.PlantAdjustMutationMod>
{
    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<Shared.EntityEffects.Effects.Botany.PlantAttributes.PlantAdjustMutationMod> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        entity.Comp.MutationMod += args.Effect.Amount;
    }
}
