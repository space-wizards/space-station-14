using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the health of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustHealthEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustHealth>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustHealth> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantHolder.AdjustsHealth(entity.Owner, args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustHealth : BasePlantAdjustAttribute<PlantAdjustHealth>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-health";
}
