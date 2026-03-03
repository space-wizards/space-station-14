using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts a plant's endurance (max health).
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustEnduranceEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustEndurance>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustEndurance> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plant.AdjustEndurance(entity.AsNullable(), (int)args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustEndurance : BasePlantAdjustAttribute<PlantAdjustEndurance>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-endurance";
}
