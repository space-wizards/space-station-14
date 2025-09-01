using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Botany;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Kitchen.Components;

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
            && _prototypeManager.TryIndex(comp.SeedId, out SeedPrototype? protoSeed))
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
            && _prototypeManager.TryIndex(comp.SeedId, out SeedPrototype? protoSeed))
        {
            seed = protoSeed;
            return true;
        }

        seed = null;
        return false;
    }

    public PlantTraitsComponent? GetPlantTraits(SeedData seed)
    {
        return seed.GrowthComponents.PlantTraits;
    }

    public HarvestComponent? GetHarvestComponent(SeedData seed)
    {
        return seed.GrowthComponents.Harvest;
    }

    private void OnExamined(EntityUid uid, SeedComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetSeed(component, out var seed))
            return;

        var traits = GetPlantTraits(seed);
        if (traits == null)
            return;

        using (args.PushGroup(nameof(SeedComponent), 1))
        {
            var name = Loc.GetString(seed.DisplayName);
            args.PushMarkup(Loc.GetString($"seed-component-description", ("seedName", name)));
            args.PushMarkup(Loc.GetString($"seed-component-plant-yield-text", ("seedYield", traits.Yield)));
            args.PushMarkup(Loc.GetString($"seed-component-plant-potency-text", ("seedPotency", traits.Potency)));
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

    public IEnumerable<EntityUid> AutoHarvest(SeedData proto, EntityCoordinates position, EntityUid? plantEntity = null)
    {
        if (position.IsValid(EntityManager) &&
            proto.ProductPrototypes.Count > 0)
        {
            if (proto.HarvestLogImpact != null)
                _adminLogger.Add(LogType.Botany, proto.HarvestLogImpact.Value, $"Auto-harvested {Loc.GetString(proto.Name):seed} at Pos:{position}.");

            return GenerateProduct(proto, position, plantEntity);
        }

        return Enumerable.Empty<EntityUid>();
    }

    public IEnumerable<EntityUid> Harvest(SeedData proto, EntityUid user, EntityUid? plantEntity = null)
    {
        var traits = GetPlantTraits(proto);
        if (traits == null || proto.ProductPrototypes.Count == 0 || traits.Yield <= 0)
        {
            _popupSystem.PopupCursor(Loc.GetString("botany-harvest-fail-message"), user, PopupType.Medium);
            return Enumerable.Empty<EntityUid>();
        }

        var name = Loc.GetString(proto.DisplayName);
        _popupSystem.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", name)), user, PopupType.Medium);

        if (proto.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, proto.HarvestLogImpact.Value, $"{ToPrettyString(user):player} harvested {Loc.GetString(proto.Name):seed} at Pos:{Transform(user).Coordinates}.");

        return GenerateProduct(proto, Transform(user).Coordinates, plantEntity);
    }

    public IEnumerable<EntityUid> GenerateProduct(SeedData proto, EntityCoordinates position, EntityUid? plantEntity = null)
    {
        var traits = GetPlantTraits(proto);
        if (traits == null)
            return Enumerable.Empty<EntityUid>();

        var yieldMod = Comp<PlantHolderComponent>(plantEntity!.Value).YieldMod;
        var totalYield = 0;

        if (traits.Yield > -1)
        {
            if (yieldMod < 0)
                totalYield = traits.Yield;
            else
                totalYield = traits.Yield * yieldMod;

            totalYield = Math.Max(1, totalYield);
        }

        var products = new List<EntityUid>();

        if (totalYield > 1)
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

            _appearance.SetData(entity, ProduceVisuals.Potency, traits.Potency);

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
        var traits = GetPlantTraits(proto);
        if (traits == null)
            return true;

        return !traits.Ligneous || traits.Ligneous && held != null && HasComp<SharpComponent>(held);
    }

    #endregion
}
