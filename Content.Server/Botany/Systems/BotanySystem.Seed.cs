using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ProduceComponent, ExaminedEvent>(OnProduceExamined);
    }

    private void OnExamined(EntityUid uid, SeedComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetPlantComponent<PlantComponent>(component.PlantData, component.PlantProtoId, out var plant)
            || !TryGetPlantComponent<PlantDataComponent>(component.PlantData, component.PlantProtoId, out var plantData))
            return;

        using (args.PushGroup(nameof(SeedComponent), 1))
        {
            var name = Loc.GetString(plantData.DisplayName);
            args.PushMarkup(Loc.GetString("seed-component-description", ("seedName", name)));
            args.PushMarkup(Loc.GetString("seed-component-plant-yield-text", ("seedYield", plant.Yield)));
            args.PushMarkup(Loc.GetString("seed-component-plant-potency-text", ("seedPotency", plant.Potency)));
        }
    }

    /// <summary>
    /// Tries to get a plant component from a snapshot or prototype.
    /// </summary>
    /// <typeparam name="T">The type of component to get.</typeparam>
    /// <param name="snapshot">The snapshot to get the component from.</param>
    /// <param name="plantProtoId">The prototype ID to get the component from.</param>
    /// <param name="plant">The plant component if found.</param>
    [PublicAPI]
    public bool TryGetPlantComponent<T>(ComponentRegistry? snapshot, EntProtoId? plantProtoId, [NotNullWhen(true)] out T? plant)
        where T : class, IComponent, new()
    {
        plant = null;

        if (snapshot != null && snapshot.TryGetComponent(_componentFactory, out plant))
            return true;

        if (plantProtoId == null)
            return false;

        if (!_prototypeManager.TryIndex(plantProtoId.Value, out var proto))
            return false;

        return proto.TryGetComponent(out plant, _componentFactory);
    }

    /// <summary>
    /// Clones a component snapshot of a plant.
    /// </summary>
    /// <param name="source">The entity to clone the snapshot from.</param>
    /// <param name="cloneLifecycle">If true, also clone lifecycle state into the snapshot.</param>
    [PublicAPI]
    public ComponentRegistry ClonePlantSnapshotData(EntityUid source, bool cloneLifecycle = false)
    {
        var snap = new ComponentRegistry();

        var typesToCopy = new[]
        {
            typeof(PlantComponent),
            typeof(PlantChemicalsComponent),
            typeof(PlantHarvestComponent),
            typeof(PlantTraitsComponent),
            typeof(BasicGrowthComponent),
            typeof(AtmosphericGrowthComponent),
            typeof(ConsumeExudeGasGrowthComponent),
            typeof(WeedPestGrowthComponent),
            typeof(UnviableGrowthComponent),
            typeof(PlantToxinsComponent)
        };

        foreach (var type in typesToCopy)
        {
            if (!EntityManager.TryGetComponent(source, type, out var comp))
                continue;

            if (comp is not Component component)
                continue;

            var copied = _serialization.CreateCopy(component, notNullableOverride: true);
            var compName = _componentFactory.GetComponentName(type);
            snap[compName] = new EntityPrototype.ComponentRegistryEntry(copied, []);
        }

        if (cloneLifecycle && TryComp<PlantHolderComponent>(source, out var holder))
        {
            var copiedHolder = _serialization.CreateCopy(holder, notNullableOverride: true);
            var holderName = _componentFactory.GetComponentName<PlantHolderComponent>();
            snap[holderName] = new EntityPrototype.ComponentRegistryEntry(copiedHolder, []);
        }

        return snap;
    }

    /// <summary>
    /// Applies a component snapshot to a plant.
    /// </summary>
    /// <param name="plant">The plant to apply the snapshot to.</param>
    /// <param name="snapshot">The component snapshot to apply.</param>
    [PublicAPI]
    public void ApplyPlantSnapshotData(EntityUid plant, ComponentRegistry snapshot)
    {
        foreach (var (_, entry) in snapshot)
        {
            if (entry.Component is not Component component)
                continue;

            var copied = _serialization.CreateCopy(component, notNullableOverride: true);
            EntityManager.AddComponent(plant, copied, overwrite: true);
        }
    }

    /// <summary>
    /// Spawns a seed packet that stores a component snapshot of <paramref name="sourcePlant"/>.
    /// </summary>
    [PublicAPI]
    public EntityUid SpawnSeedPacketFromPlant(EntityUid sourcePlant, EntityCoordinates coords, EntityUid user, float? healthOverride = null)
    {
        if (!TryComp<PlantDataComponent>(sourcePlant, out var plantData))
            return EntityUid.Invalid;

        var protoId = MetaData(sourcePlant).EntityPrototype?.ID;
        var snapshot = ClonePlantSnapshotData(sourcePlant);

        return SpawnSeedPacketInternal(plantData, protoId, snapshot, coords, user, healthOverride);
    }

    /// <summary>
    /// Spawns a seed packet that stores a component snapshot of <paramref name="snapshot"/>.
    /// </summary
    [PublicAPI]
    public EntityUid SpawnSeedPacketFromSnapshot(ComponentRegistry snapshot, EntProtoId? plantProtoId, EntityCoordinates coords, EntityUid user, float? healthOverride = null)
    {
        if (!TryGetPlantComponent<PlantDataComponent>(snapshot, plantProtoId, out var plantData))
            return EntityUid.Invalid;

        return SpawnSeedPacketInternal(plantData, plantProtoId, snapshot, coords, user, healthOverride);
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
    private EntityUid SpawnSeedPacketInternal(
        PlantDataComponent plantData,
        EntProtoId? plantProtoId,
        ComponentRegistry? snapshot,
        EntityCoordinates coords,
        EntityUid user,
        float? healthOverride)
    {
        var seedItem = Spawn(plantData.PacketPrototype, coords);
        var seedComp = EnsureComp<SeedComponent>(seedItem);
        seedComp.PlantProtoId = plantProtoId;
        seedComp.PlantData = snapshot != null
            ? _serialization.CreateCopy(snapshot, notNullableOverride: true)
            : null;
        seedComp.HealthOverride = healthOverride;

        var name = Loc.GetString(plantData.DisplayName);
        var noun = Loc.GetString(plantData.Noun);
        _metaData.SetEntityName(seedItem, Loc.GetString("botany-seed-packet-name", ("seedName", name), ("seedNoun", noun)));

        _hands.TryPickupAnyHand(user, seedItem);
        return seedItem;
    }
}
