using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Changes the planted plant's species by replacing the plant entity with a new entity spawned from one
/// of the current plant's <see cref="PlantDataComponent.MutationPrototypes"/>.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateSpeciesChangeEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateSpeciesChange>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateSpeciesChange> args)
    {
        // No predict random.
        if (_net.IsClient)
            return;

        if (!TryComp<PlantDataComponent>(entity, out var oldPlantData)
            || oldPlantData.MutationPrototypes.Count == 0)
            return;

        var newPlantEnt = _random.Pick(oldPlantData.MutationPrototypes);
        _mutation.SpeciesChange(entity.Owner, newPlantEnt);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateSpeciesChange : EntityEffectBase<PlantMutateSpeciesChange>;
