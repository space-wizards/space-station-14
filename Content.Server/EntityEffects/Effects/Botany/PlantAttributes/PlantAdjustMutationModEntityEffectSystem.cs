using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the mutation mod of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustMutationModEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustMutationMod>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustMutationMod> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantHolder.AdjustsMutationMod(entity.Owner, args.Effect.Amount);
    }
}
