using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAdjustToxinsEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantAdjustToxins>
{
    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantAdjustToxins> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead)
            return;

        entity.Comp.Toxins += args.Effect.Amount;
    }
}
