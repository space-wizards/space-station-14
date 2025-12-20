using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Changes the planted plant's species by replacing the plant entity with a new entity spawned from one
/// of the current plant's <see cref="PlantDataComponent.MutationPrototypes"/>.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateSpeciesChangeEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantMutateSpeciesChange>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantMutateSpeciesChange> args)
    {
        if (!_plantTray.TryGetPlant(entity.AsNullable(), out var plant)
            ||!TryComp<PlantDataComponent>(plant, out var oldPlantData)
            || oldPlantData.MutationPrototypes.Count == 0)
            return;

        var newPlantEnt = _random.Pick(oldPlantData.MutationPrototypes);
        _mutation.SpeciesChange(plant.Value, newPlantEnt, entity.AsNullable());
    }
}
