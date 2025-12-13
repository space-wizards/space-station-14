using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Botany;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ProduceComponent, ExaminedEvent>(OnProduceExamined);
    }

    public bool TryGetSeed(SeedComponent comp, [NotNullWhen(true)] out SeedData? seed)
    {
        if (comp.Seed != null)
        {
            seed = comp.Seed;
            return true;
        }

        if (comp.SeedId != null
            && _prototypeManager.TryIndex(comp.SeedId, out var protoSeed))
        {
            seed = protoSeed.Clone();
            return true;
        }

        seed = null;
        return false;
    }

    public bool TryGetSeed(ProduceComponent comp, [NotNullWhen(true)] out SeedData? seed)
    {
        if (comp.Seed != null)
        {
            seed = comp.Seed;
            return true;
        }

        if (comp.SeedId != null
            && _prototypeManager.TryIndex(comp.SeedId, out var protoSeed))
        {
            seed = protoSeed;
            return true;
        }

        seed = null;
        return false;
    }

    /// TODO: Delete after plants transition to entities
    public static PlantComponent? GetPlantComponent(SeedData seed)
    {
        return seed.GrowthComponents.Plant;
    }

    /// TODO: Delete after plants transition to entities
    public static PlantTraitsComponent? GetPlantTraitsComponent(SeedData seed)
    {
        return seed.GrowthComponents.PlantTraits;
    }

    private void OnExamined(EntityUid uid, SeedComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetSeed(component, out var seed))
            return;

        var plant = GetPlantComponent(seed);
        if (plant == null)
            return;

        using (args.PushGroup(nameof(SeedComponent), 1))
        {
            var name = Loc.GetString(seed.DisplayName);
            args.PushMarkup(Loc.GetString($"seed-component-description", ("seedName", name)));
            args.PushMarkup(Loc.GetString($"seed-component-plant-yield-text", ("seedYield", plant.Yield)));
            args.PushMarkup(Loc.GetString($"seed-component-plant-potency-text", ("seedPotency", plant.Potency)));
        }
    }

    #region SeedPrototype prototype stuff

    /// <summary>
    /// Spawns a new seed packet on the floor at a position, then tries to put it in the user's hands if possible.
    /// </summary>
    public EntityUid SpawnSeedPacket(SeedData proto, EntityCoordinates coords, EntityUid user, float? healthOverride = null)
    {
        var seed = Spawn(proto.PacketPrototype, coords);
        var seedComp = EnsureComp<SeedComponent>(seed);
        seedComp.Seed = proto.Clone();
        seedComp.HealthOverride = healthOverride;

        var name = Loc.GetString(proto.Name);
        var noun = Loc.GetString(proto.Noun);
        var val = Loc.GetString("botany-seed-packet-name", ("seedName", name), ("seedNoun", noun));
        _metaData.SetEntityName(seed, val);

        // try to automatically place in user's other hand
        _hands.TryPickupAnyHand(user, seed);
        return seed;
    }

    public IEnumerable<EntityUid> AutoHarvest(SeedData proto, EntityCoordinates position, EntityUid plantEntity)
    {
        if (position.IsValid(EntityManager) &&
            proto.ProductPrototypes.Count > 0)
        {
            if (proto.HarvestLogImpact != null)
                _adminLogger.Add(LogType.Botany, proto.HarvestLogImpact.Value, $"Auto-harvested {Loc.GetString(proto.Name):seed} at Pos:{position}.");

            return GenerateProduct(proto, position, plantEntity);
        }

        return [];
    }

    public IEnumerable<EntityUid> Harvest(SeedData proto, EntityUid user, EntityUid plantEntity)
    {
        var plant = GetPlantComponent(proto);
        if (plant == null || proto.ProductPrototypes.Count == 0 || plant.Yield <= 0)
        {
            _popupSystem.PopupCursor(Loc.GetString("botany-harvest-fail-message"), user, PopupType.Medium);
            return [];
        }

        var name = Loc.GetString(proto.DisplayName);
        _popupSystem.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", name)), user, PopupType.Medium);

        if (proto.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, proto.HarvestLogImpact.Value, $"{ToPrettyString(user):player} harvested {Loc.GetString(proto.Name):seed} at Pos:{Transform(user).Coordinates}.");

        return GenerateProduct(proto, Transform(user).Coordinates, plantEntity);
    }

    public IEnumerable<EntityUid> GenerateProduct(SeedData proto, EntityCoordinates position, EntityUid plantEntity)
    {
        var plant = GetPlantComponent(proto);
        if (plant == null)
            return [];

        var yieldMod = Comp<PlantHolderComponent>(plantEntity).YieldMod;
        var harvest = Comp<PlantHarvestComponent>(plantEntity);

        var totalYield = 0;

        if (plant.Yield > -1)
        {
            if (yieldMod < 0)
                totalYield = plant.Yield;
            else
                totalYield = plant.Yield * yieldMod;

            totalYield = Math.Max(1, totalYield);
        }

        var products = new List<EntityUid>();

        if (totalYield > 1 || harvest.HarvestRepeat != HarvestType.NoRepeat)
            proto.Unique = false;

        for (var i = 0; i < totalYield; i++)
        {
            var product = _robustRandom.Pick(proto.ProductPrototypes);

            var entity = Spawn(product, position);
            _randomHelper.RandomOffset(entity, 0.25f);
            products.Add(entity);

            var produce = EnsureComp<ProduceComponent>(entity);

            produce.Seed = proto.Clone();

            ProduceGrown(entity, produce);

            _appearance.SetData(entity, ProduceVisuals.Potency, plant.Potency);

            if (proto.Mysterious)
            {
                var metaData = MetaData(entity);
                _metaData.SetEntityName(entity, metaData.EntityName + "?", metaData);
                _metaData.SetEntityDescription(entity,
                    metaData.EntityDescription + " " + Loc.GetString("botany-mysterious-description-addon"), metaData);
            }
        }

        return products;
    }

    public bool CanHarvest(SeedData proto, EntityUid? held = null)
    {
        var traits = GetPlantTraitsComponent(proto);
        if (traits == null)
            return true;

        return !traits.Ligneous || traits.Ligneous && held != null && HasComp<SharpComponent>(held);
    }

    #endregion
}
