using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the toxins of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustToxinsEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustToxins>
{
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustToxins> args)
    {
        if (!_plantTray.TryGetPlant(entity.AsNullable(), out _))
            return;

        entity.Comp.Toxins += args.Effect.Amount;
    }
}
