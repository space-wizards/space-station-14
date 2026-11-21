using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.EntityConditions;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Changes the planted plant's species by replacing the plant entity with a new entity spawned from one
/// of the effect's target prototypes.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateSpeciesChangeEntityEffectSystem : EntityEffectSystem<PlantDataComponent, PlantMutateSpeciesChange>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    protected override void Effect(Entity<PlantDataComponent> entity, ref EntityEffectEvent<PlantMutateSpeciesChange> args)
    {
        if (!_net.IsServer)
            return;

        var totalWeight = 0f;
        foreach (var entry in args.Effect.SpeciesOptions)
        {
            if (entry.Weight <= 0 || !float.IsFinite(entry.Weight))
                continue;

            if (!_conditions.TryConditions(entity.Owner, entry.Conditions, args.User))
                continue;

            totalWeight += entry.Weight;
        }

        if (totalWeight <= 0f)
            return;

        var selectedWeight = _random.NextFloat(totalWeight);
        var accumulatedWeight = 0f;
        foreach (var entry in args.Effect.SpeciesOptions)
        {
            if (entry.Weight <= 0 || !float.IsFinite(entry.Weight))
                continue;

            if (!_conditions.TryConditions(entity.Owner, entry.Conditions, args.User))
                continue;

            accumulatedWeight += entry.Weight;
            if (accumulatedWeight < selectedWeight)
                continue;

            _mutation.SpeciesChange(entity.Owner, entry.Prototype);
            return;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateSpeciesChange : EntityEffectBase<PlantMutateSpeciesChange>
{
    /// <summary>
    /// Weighted plant prototypes to choose from when changing species, with entityConditions.
    /// </summary>
    [DataField]
    public List<PlantSpeciesChangeEntry> SpeciesOptions = [];
}

/// <summary>
/// A possible destination species with weights and conditions.
/// </summary>
[DataDefinition]
public sealed partial class PlantSpeciesChangeEntry
{
    /// <summary>
    /// The plant to change into.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// Weighting for the mutation.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Conditions necessary to mutate.
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;
}
