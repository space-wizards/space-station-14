using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the mutation mod of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustMutationModEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustMutationMod>
{
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustMutationMod> args)
    {
        if (!_plantTray.TryGetPlant(entity.AsNullable(), out var plant))
            return;

        if (!TryComp<PlantHolderComponent>(plant, out var plantHolder))
            return;

        if (plantHolder.Dead)
            return;

        plantHolder.MutationMod += args.Effect.Amount;
    }
}
