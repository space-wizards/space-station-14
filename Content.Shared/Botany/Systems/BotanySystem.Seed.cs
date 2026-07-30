using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cloning;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Random;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

public sealed partial class BotanySystem : EntitySystem
{
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PlantSystem _plant = default!;
    [Dependency] private RandomHelperSystem _randomHelper = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedCloningSystem _cloning = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public readonly ProtoId<CloningSettingsPrototype> SettingsId = "PlantClone";
    public readonly ProtoId<CloningSettingsPrototype> LifecycleSettingsId = "PlantLifecycleClone";

    [SubscribeLocalEvent]
    private void OnExamined(Entity<SeedComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetPlantComponent<PlantComponent>(ent.Comp.PlantData, ent.Comp.PlantProtoId, out var plant))
            return;

        using (args.PushGroup(nameof(SeedComponent), 1))
        {
            var name = Loc.GetString(plant.Name);
            args.PushMarkup(Loc.GetString("seed-component-description", ("seedName", name)));
            args.PushMarkup(_plant.GetPlantStateMarkup(ent.Owner, plant));
        }
    }

    [SubscribeLocalEvent]
    private void OnSeedShutdown(Entity<SeedComponent> ent, ref ComponentShutdown args)
    {
        DeletePlantSnapshot(ent.Comp.PlantData);
    }

    [SubscribeLocalEvent]
    private void OnProduceShutdown(Entity<ProduceComponent> ent, ref ComponentShutdown args)
    {
        DeletePlantSnapshot(ent.Comp.PlantData);
    }

    [SubscribeLocalEvent]
    private void OnSwabShutdown(Entity<BotanySwabComponent> ent, ref ComponentShutdown args)
    {
        DeletePlantSnapshot(ent.Comp.PlantData);
    }

    /// <summary>
    /// Tries to get a plant component from a snapshot or prototype.
    /// </summary>
    /// <typeparam name="T">The type of component to get.</typeparam>
    /// <param name="snapshot">The snapshot to get the component from.</param>
    /// <param name="plantProtoId">The prototype ID to get the component from.</param>
    /// <param name="plant">The plant component if found.</param>
    [PublicAPI]
    public bool TryGetPlantComponent<T>(EntityUid? snapshot, EntProtoId? plantProtoId, [NotNullWhen(true)] out T? plant)
        where T : class, IComponent, new()
    {
        plant = null;

        if (snapshot != null && TryComp(snapshot, out plant))
            return true;

        if (plantProtoId == null)
            return false;

        if (!ProtoMan.TryIndex(plantProtoId.Value, out var proto))
            return false;

        return proto.TryComp(out plant, _componentFactory);
    }

    /// <summary>
    /// Clones a component snapshot of a plant.
    /// </summary>
    /// <param name="source">The entity to clone the snapshot from.</param>
    /// <param name="parent">The entity that should own the snapshot, if any.</param>
    /// <param name="cloneLifecycle">If true, also clone lifecycle state into the snapshot.</param>
    [PublicAPI]
    public EntityUid? ClonePlantSnapshotData(EntityUid source, EntityUid? parent = null, bool cloneLifecycle = false)
    {
        var settingsId = cloneLifecycle ? LifecycleSettingsId : SettingsId;
        if (!ProtoMan.TryIndex(settingsId, out var settings))
            return null;

        var snapshot = EntityManager.CreateEntityUninitialized(null);
        _cloning.CloneComponents(source, snapshot, settings);
        EntityManager.InitializeAndStartEntity(snapshot, doMapInit: false);

        if (parent is { } parentUid)
            _transform.SetParent(snapshot, parentUid);

        return snapshot;
    }

    /// <summary>
    /// Deletes a stored plant snapshot, if one exists.
    /// </summary>
    [PublicAPI]
    public void DeletePlantSnapshot(EntityUid? snapshot)
    {
        if (snapshot == null)
            return;

        PredictedQueueDel(snapshot.Value);
    }

    /// <summary>
    /// Applies the component data stored in a plant snapshot to a target entity.
    /// </summary>
    /// <param name="snapshot">The snapshot entity to copy component data from.</param>
    /// <param name="target">The entity to apply the snapshot data to.</param>
    /// <param name="cloneLifecycle">If true, also copy lifecycle state.</param>
    [PublicAPI]
    public void ApplyPlantSnapshotData(EntityUid? snapshot, EntityUid target, bool cloneLifecycle = false)
    {
        if (snapshot == null)
            return;

        var settingsId = cloneLifecycle ? LifecycleSettingsId : SettingsId;
        if (!ProtoMan.TryIndex(settingsId, out var settings))
            return;

        _cloning.CloneComponents(snapshot.Value, target, settings);
    }

    /// <summary>
    /// Internal method to spawn a seed packet from a plant component.
    /// </summary>
    /// <param name="plant">The plant component to spawn.</param>
    /// <param name="plantProtoId">The plant prototype ID to store in the seed component.</param>
    /// <param name="snapshot">The component snapshot to store in the seed component.</param>
    /// <param name="coords">The coordinates to spawn the seed packet at.</param>
    /// <param name="user">The user who is spawning the seed packet.</param>
    /// <param name="healthOverride">The health override to store in the seed component.</param>
    /// <returns>The spawned seed packet entity.</returns>
    [PublicAPI]
    public EntityUid SpawnSeedPacket(
        PlantComponent plant,
        EntProtoId plantProtoId,
        EntityUid? snapshot,
        EntityCoordinates coords,
        EntityUid user,
        float? healthOverride = null)
    {
        var seedItem = PredictedSpawnAtPosition(plant.PacketPrototype, coords);
        var seedComp = EnsureComp<SeedComponent>(seedItem);
        seedComp.PlantProtoId = plantProtoId;
        seedComp.PlantData = snapshot.HasValue
            ? ClonePlantSnapshotData(snapshot.Value, parent: seedItem)
            : null;
        seedComp.HealthOverride = healthOverride;
        Dirty(seedItem, seedComp);

        var name = Loc.GetString(plant.Name);
        var noun = Loc.GetString(plant.Noun);
        _metaData.SetEntityName(seedItem, Loc.GetString("botany-seed-packet-name", ("seedName", name), ("seedNoun", noun)));

        _hands.TryPickupAnyHand(user, seedItem);
        return seedItem;
    }
}
