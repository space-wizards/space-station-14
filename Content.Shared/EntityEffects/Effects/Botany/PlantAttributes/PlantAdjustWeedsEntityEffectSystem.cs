using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the weeds of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class
    PlantAdjustWeedsEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustWeeds>
{
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustWeeds> args)
    {
        if (!_plantTray.TryGetAlivePlant(entity.AsNullable(), out _))
            return;

        _plantTray.AdjustWeed(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustWeeds : BasePlantAdjustAttribute<PlantAdjustWeeds>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-weeds";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
