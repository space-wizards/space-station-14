using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the toxins of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustToxinsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustToxins>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustToxins> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantHolder.AdjustsToxins(entity.Owner, args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustToxins : BasePlantAdjustAttribute<PlantAdjustToxins>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-toxins";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
