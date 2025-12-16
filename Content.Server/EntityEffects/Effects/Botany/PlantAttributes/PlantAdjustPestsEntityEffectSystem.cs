using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the pests of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustPestsEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustPests>
{
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustPests> args)
    {
        if (!_plantTray.HasPlant(entity.AsNullable()))
            return;

        var plantUid = entity.Comp.PlantEntity!.Value;
        if (TryComp<PlantHolderComponent>(plantUid, out var plantHolder) && plantHolder.Dead)
            return;

        entity.Comp.PestLevel += args.Effect.Amount;
    }
}
