using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly CollisionWakeSystem _colWakeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedComponent, ExaminedEvent>(OnExamined);
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
            seed = protoSeed;
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

    private void OnExamined(EntityUid uid, SeedComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetSeed(component, out var seed))
            return;

        var name = Loc.GetString(seed.DisplayName);
        args.PushMarkup(Loc.GetString($"seed-component-description", ("seedName", name)));
        args.PushMarkup(Loc.GetString($"seed-component-plant-yield-text", ("seedYield", seed.Yield)));
        args.PushMarkup(Loc.GetString($"seed-component-plant-potency-text", ("seedPotency", seed.Potency)));
    }

    #region SeedPrototype prototype stuff

    /// <summary>
    /// Spawns a new seed packet on the floor at a position, then tries to put it in the user's hands if possible.
    /// </summary>
    public EntityUid SpawnSeedPacket(SeedData proto, EntityCoordinates coords, EntityUid user)
    {
        var seed = Spawn(proto.PacketPrototype, coords);
        var seedComp = EnsureComp<SeedComponent>(seed);
        seedComp.Seed = proto;

        var name = Loc.GetString(proto.Name);
        var noun = Loc.GetString(proto.Noun);
        var val = Loc.GetString("botany-seed-packet-name", ("seedName", name), ("seedNoun", noun));
        _metaData.SetEntityName(seed, val);

        // try to automatically place in user's other hand
        _hands.TryPickupAnyHand(user, seed);
        return seed;
    }

    public IEnumerable<EntityUid> AutoHarvest(SeedData proto, EntityCoordinates position, int yieldMod = 1)
    {
        if (position.IsValid(EntityManager) &&
            proto.ProductPrototypes.Count > 0)
            return GenerateProduct(proto, position, yieldMod);

        return Enumerable.Empty<EntityUid>();
    }

    public IEnumerable<EntityUid> Harvest(SeedData proto, EntityUid user, int yieldMod = 1)
    {
        if (proto.ProductPrototypes.Count == 0 || proto.Yield <= 0)
        {
            _popupSystem.PopupCursor(Loc.GetString("botany-harvest-fail-message"), user, PopupType.Medium);
            return Enumerable.Empty<EntityUid>();
        }

        var name = Loc.GetString(proto.DisplayName);
        _popupSystem.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", name)), user, PopupType.Medium);
        return GenerateProduct(proto, Transform(user).Coordinates, yieldMod);
    }

    public IEnumerable<EntityUid> GenerateProduct(SeedData proto, EntityCoordinates position, int yieldMod = 1)
    {
        var totalYield = 0;
        if (proto.Yield > -1)
        {
            if (yieldMod < 0)
                totalYield = proto.Yield;
            else
                totalYield = proto.Yield * yieldMod;

            totalYield = Math.Max(1, totalYield);
        }

        var products = new List<EntityUid>();

        if (totalYield > 1 || proto.HarvestRepeat != HarvestType.NoRepeat)
            proto.Unique = false;

        for (var i = 0; i < totalYield; i++)
        {
            var product = _robustRandom.Pick(proto.ProductPrototypes);

            var entity = Spawn(product, position);
            entity.RandomOffset(0.25f);
            products.Add(entity);

            var produce = EnsureComp<ProduceComponent>(entity);

            produce.Seed = proto;
            ProduceGrown(entity, produce);

            _appearance.SetData(entity, ProduceVisuals.Potency, proto.Potency);

            if (proto.Mysterious)
            {
                var metaData = MetaData(entity);
                _metaData.SetEntityName(entity, metaData.EntityName + "?", metaData);
                _metaData.SetEntityDescription(entity,
                    metaData.EntityDescription + " " + Loc.GetString("botany-mysterious-description-addon"), metaData);
            }

            if (proto.Bioluminescent)
            {
                var light = _light.EnsureLight(entity);
                _light.SetRadius(entity, proto.BioluminescentRadius, light);
                _light.SetColor(entity, proto.BioluminescentColor, light);
                // TODO: Ayo why you copy-pasting code between here and plantholder?
                _light.SetCastShadows(entity, false, light); // this is expensive, and botanists make lots of plants
            }

            if (proto.Slip)
            {
                var slippery = EnsureComp<SlipperyComponent>(entity);
                Dirty(entity, slippery);
                EnsureComp<StepTriggerComponent>(entity);
                // Need a fixture with a slip layer in order to actually do the slipping
                var fixtures = EnsureComp<FixturesComponent>(entity);
                var body = EnsureComp<PhysicsComponent>(entity);
                var shape = fixtures.Fixtures["fix1"].Shape;
                _fixtureSystem.TryCreateFixture(entity, shape, "slips", 1, false, (int) CollisionGroup.SlipLayer, manager: fixtures, body: body);
                // Need to disable collision wake so that mobs can collide with and slip on it
                var collisionWake = EnsureComp<CollisionWakeComponent>(entity);
                _colWakeSystem.SetEnabled(entity, false, collisionWake);
            }
        }

        return products;
    }

    public bool CanHarvest(SeedData proto, EntityUid? held = null)
    {
        return !proto.Ligneous || proto.Ligneous && held != null && HasComp<SharpComponent>(held);
    }

    #endregion
}
