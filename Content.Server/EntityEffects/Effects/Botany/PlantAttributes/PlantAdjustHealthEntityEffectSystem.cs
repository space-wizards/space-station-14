using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the health of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustHealthEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustHealth>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustHealth> args)
    {
        if (!_plantTray.HasPlant(entity.AsNullable()))
            return;

        var plantUid = entity.Comp.PlantEntity!.Value;
        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder))
            return;

        if (plantHolder.Dead)
            return;

        plantHolder.Health += args.Effect.Amount;
        _plantHolder.CheckHealth(plantUid);
    }
}
