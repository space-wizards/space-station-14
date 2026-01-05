using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the pests of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustPestsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustPests>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustPests> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantHolder.AdjustsPests(entity.Owner, args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustPests : BasePlantAdjustAttribute<PlantAdjustPests>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-pests";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
