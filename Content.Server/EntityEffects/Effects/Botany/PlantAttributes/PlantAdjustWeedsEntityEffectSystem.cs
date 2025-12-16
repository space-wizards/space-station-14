
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the weeds of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustWeedsEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustWeeds>
{
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustWeeds> args)
    {
        if (!_plantTray.HasPlant(entity.AsNullable()))
            return;

        entity.Comp.WeedLevel += args.Effect.Amount;
    }
}
