using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the toxins of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustToxinsEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustToxins>
{
    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustToxins> args)
    {
        if (entity.Comp.PlantEntity == null || Deleted(entity.Comp.PlantEntity))
            return;

        entity.Comp.Toxins += args.Effect.Amount;
    }
}
