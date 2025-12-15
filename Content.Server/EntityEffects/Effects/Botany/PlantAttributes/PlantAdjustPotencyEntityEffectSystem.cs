using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that sets plant potency.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustPotencyEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustPotency>
{
    [Dependency] private readonly PlantSystem _plant = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustPotency> args)
    {
        if (entity.Comp.PlantEntity == null || Deleted(entity.Comp.PlantEntity))
            return;

        var plantUid = entity.Comp.PlantEntity.Value;
        if (!TryComp<PlantComponent>(plantUid, out var plant) || !TryComp<PlantHolderComponent>(plantUid, out var plantHolder) || plantHolder.Dead)
            return;

        _plant.AdjustPotency((plantUid, plant), args.Effect.Amount);
    }
}
