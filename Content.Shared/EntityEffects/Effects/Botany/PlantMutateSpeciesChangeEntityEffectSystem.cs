using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Changes the planted plant's species by replacing the plant entity with a new entity spawned from one
/// of the current plant's <see cref="PlantDataComponent.MutationPrototypes"/>.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateSpeciesChangeEntityEffectSystem : EntityEffectSystem<PlantDataComponent, PlantMutateSpeciesChange>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MutationSystem _mutation = default!;

    protected override void Effect(Entity<PlantDataComponent> entity, ref EntityEffectEvent<PlantMutateSpeciesChange> args)
    {
        if (entity.Comp.MutationPrototypes.Count == 0)
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
        var newPlantEnt = random.Pick(entity.Comp.MutationPrototypes);
        _mutation.SpeciesChange(entity.Owner, newPlantEnt);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateSpeciesChange : EntityEffectBase<PlantMutateSpeciesChange>;
