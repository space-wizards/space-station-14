using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAdjustPotencyEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantAdjustPotency>
{
    protected override void Effect(Entity<PlantHolderComponent> entity, PlantAdjustPotency effect, EntityEffectData data)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        entity.Comp.Seed.Potency = Math.Max(entity.Comp.Seed.Potency + effect.Amount, 1);
    }
}
