using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that increments plant age / growth cycle.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAffectGrowthEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAffectGrowth>
{
    [Dependency] private readonly BasicGrowthSystem _plantGrowth = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAffectGrowth> args)
    {
        if (!_plantTray.TryGetPlant(entity.AsNullable(), out var plant))
            return;

        _plantGrowth.AffectGrowth(plant.Value, (int)args.Effect.Amount);
    }
}
