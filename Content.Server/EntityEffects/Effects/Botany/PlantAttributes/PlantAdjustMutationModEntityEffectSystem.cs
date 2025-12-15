using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the mutation mod of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustMutationModEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustMutationMod>
{
    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustMutationMod> args)
    {
        if (entity.Comp.PlantEntity == null || Deleted(entity.Comp.PlantEntity))
            return;

        var plantUid = entity.Comp.PlantEntity.Value;
        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder))
            return;

        if (plantHolder.Dead)
            return;

        plantHolder.MutationMod += args.Effect.Amount;
    }
}
