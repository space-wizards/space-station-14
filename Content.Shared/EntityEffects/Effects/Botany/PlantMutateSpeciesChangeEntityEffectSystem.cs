using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Changes the planted plant's species by replacing the plant entity with a new entity spawned from one
/// of the effect's target prototypes.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateSpeciesChangeEntityEffectSystem : EntityEffectSystem<PlantDataComponent, PlantMutateSpeciesChange>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;

    protected override void Effect(Entity<PlantDataComponent> entity, ref EntityEffectEvent<PlantMutateSpeciesChange> args)
    {
        if (args.Effect.Prototypes.Count == 0)
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
        var newPlantEnt = random.Pick(args.Effect.Prototypes);
        _mutation.SpeciesChange(entity.Owner, newPlantEnt);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateSpeciesChange : EntityEffectBase<PlantMutateSpeciesChange>
{
    /// <summary>
    /// Plant prototypes to choose from when changing species.
    /// </summary>
    [DataField]
    public List<EntProtoId> Prototypes = [];
}
