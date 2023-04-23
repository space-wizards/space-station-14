using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

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

        // TODO: Remove this in favor of the scanner
        args.PushMarkup("T: " + seed.TRA.T);
        args.PushMarkup("R: " + seed.TRA.R);
        args.PushMarkup("A: " + seed.TRA.A);
    }

    #region SeedPrototype prototype stuff

    /// <summary>
    /// Spawns a new seed packet on the floor at a position, then tries to put it in the user's hands if possible.
    /// </summary>
    public EntityUid SpawnSeedPacket(SeedData seedData, EntityCoordinates coords, EntityUid user, bool sampled = false)
    {
        // If the plant was sampled then try to match the plant's TRA match the requirements
        if (sampled){
            return SpawnSeedPacket(GetPossibleTransmutation(seedData), coords, user);
        }

        var seed = Spawn(seedData.PacketPrototype, coords);
        var seedComp = EnsureComp<SeedComponent>(seed);

        seedComp.Seed = seedData;

        if (TryComp(seed, out SpriteComponent? sprite))
        {
            // TODO visualizer
            // SeedPrototype state will always be seed. Blame the spriter if that's not the case!
            sprite.LayerSetSprite(0, new SpriteSpecifier.Rsi(seedData.PlantRsi, "seed"));
        }

        var name = Loc.GetString(seedData.Name);
        var noun = Loc.GetString(seedData.Noun);
        var val = Loc.GetString("botany-seed-packet-name", ("seedName", name), ("seedNoun", noun));
        MetaData(seed).EntityName = val;

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
                metaData.EntityName += "?";
                metaData.EntityDescription += " " + Loc.GetString("botany-mysterious-description-addon");
            }

            if (proto.Bioluminescent)
            {
                var light = EnsureComp<PointLightComponent>(entity);
                light.Radius = proto.BioluminescentRadius;
                light.Color = proto.BioluminescentColor;
                light.CastShadows = false; // this is expensive, and botanists make lots of plants
                Dirty(light);
            }

            if (proto.Slip)
            {
                var slippery = EnsureComp<SlipperyComponent>(entity);
                EntityManager.Dirty(slippery);
                EnsureComp<StepTriggerComponent>(entity);
            }
        }

        return products;
    }

    /// <summary>
    ///     Checks if the provided seed data's TRA matches any of it's transmutations TRA values and returns the transmutation's seed data
    ///     If not then returns the plants regular data
    /// </summary>
    public SeedData GetPossibleTransmutation(SeedData seedData){
        foreach(var tra_proto in seedData.PlantTransmutations){
            if (!_prototypeManager.TryIndex(tra_proto, out TransmuationPrototype? transmuation)) {
                Logger.Error($"Unknown transmutation prototype: {tra_proto}");
                continue;
            }
            if (!_prototypeManager.TryIndex(transmuation.prototype, out SeedPrototype? newSeedData)){
                Logger.Error($"Transmutation plant prototype does not exist: {transmuation.prototype}");
                continue;
            }
            if (transmuation.T == seedData.TRA.T && transmuation.R == seedData.TRA.R && transmuation.A == seedData.TRA.R){
                Logger.Info($"TRA sequences match, returning new seed data for: {newSeedData.Name}");
                return newSeedData;
            }
        }

        return seedData;
    }

    public bool CanHarvest(SeedData proto, EntityUid? held = null)
    {
        return !proto.Ligneous || proto.Ligneous && held != null && HasComp<SharpComponent>(held);
    }

    #endregion
}
