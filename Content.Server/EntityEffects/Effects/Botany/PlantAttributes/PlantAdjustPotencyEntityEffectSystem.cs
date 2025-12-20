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
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustPotency> args)
    {
        if (!_plantTray.TryGetAlivePlant(entity.AsNullable(), out var plantUid, out _)
            || !TryComp<PlantComponent>(plantUid, out var plant))
            return;

        _plant.AdjustPotency((plantUid.Value, plant), args.Effect.Amount);
    }
}
