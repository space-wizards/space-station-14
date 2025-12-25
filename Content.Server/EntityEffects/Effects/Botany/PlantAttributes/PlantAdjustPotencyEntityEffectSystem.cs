using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that sets plant potency.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustPotencyEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustPotency>
{
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustPotency> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plant.AdjustPotency(entity.AsNullable(), args.Effect.Amount);
    }
}
