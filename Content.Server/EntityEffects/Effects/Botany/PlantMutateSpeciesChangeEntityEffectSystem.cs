using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Changes the planted plant's species by replacing the plant entity with a new entity spawned from one
/// of the current plant's <see cref="PlantComponent.MutationPrototypes"/>.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateSpeciesChangeEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantMutateSpeciesChange>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly PlantTraySystem _tray = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantMutateSpeciesChange> args)
    {
        var trayUid = entity.Owner;
        var tray = entity.Comp;

        if (tray.PlantEntity == null || Deleted(tray.PlantEntity))
            return;

        var oldPlantUid = tray.PlantEntity.Value;
        if (!TryComp<PlantComponent>(oldPlantUid, out var oldPlant) || oldPlant.MutationPrototypes.Count == 0)
            return;

        if (!TryComp<PlantHolderComponent>(oldPlantUid, out var oldHolder))
            return;

        var targetProto = _random.Pick(oldPlant.MutationPrototypes);

        // Spawn new plant of target species and attach it to the tray.
        var coords = Transform(trayUid).Coordinates;
        var newPlantUid = Spawn(targetProto, coords);
        _transform.SetCoordinates(newPlantUid, coords);
        _transform.SetParent(newPlantUid, trayUid);

        // Preserve lifecycle state by copying the entire component.
        var copiedHolder = _serialization.CreateCopy(oldHolder, notNullableOverride: true);
        EntityManager.AddComponent(newPlantUid, copiedHolder, overwrite: true);
        var newHolder = copiedHolder;

        // Keep health within the new species' endurance, if applicable.
        if (TryComp<PlantComponent>(newPlantUid, out var newPlant))
            newHolder.Health = Math.Clamp(newHolder.Health, 0f, newPlant.Endurance);

        // Reset harvest state so the new species doesn't instantly become harvestable due to high age.
        if (TryComp<PlantHarvestComponent>(newPlantUid, out var newHarvest))
        {
            newHarvest.ReadyForHarvest = false;
            newHarvest.LastHarvest = newHolder.Age;
        }

        // Swap tray reference and delete old plant.
        tray.PlantEntity = newPlantUid;
        QueueDel(oldPlantUid);

        tray.UpdateSpriteAfterUpdate = true;
        _tray.UpdateSprite((trayUid, tray));
    }
}
