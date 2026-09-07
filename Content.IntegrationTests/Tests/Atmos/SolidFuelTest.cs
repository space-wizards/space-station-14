using System.Collections.Generic;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chemistry.TileReactions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.CCVar;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.IgnitionSource;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Atmos;

[TestFixture]
[TestOf(typeof(SolidFuelSystem))]
public sealed class SolidFuelTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");
    private SolidFuelSystem _fuel = default!;
    private FlammableSystem _fire = default!;
    private SharedSolutionContainerSystem _solutions = default!;
    private IConfigurationManager _config = default!;

    private static readonly ProtoId<ReagentPrototype> Water = "Water";

    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        await Server.WaitPost(() =>
        {
            _fuel = SEntMan.System<SolidFuelSystem>();
            _fire = SEntMan.System<FlammableSystem>();
            _solutions = SEntMan.System<SharedSolutionContainerSystem>();
            _config = Server.ResolveDependency<IConfigurationManager>();
        });
    }

    private EntityUid SpawnFuel(string prototype)
    {
        var uid = SEntMan.SpawnEntity(prototype, new EntityCoordinates(RelevantAtmos.Owner, 0.5f, 0.5f));
        var air = SAtmos.GetContainingMixture(uid);
        Assert.That(air, Is.Not.Null);
        air!.SetMoles(Gas.Oxygen, 20f);
        air.SetMoles(Gas.Nitrogen, 80f);
        return uid;
    }

    private EntityUid Cigarette()
    {
        var uid = SpawnFuel("Cigarette");
        SEntMan.GetComponent<SmokableComponent>(uid).State = SmokableState.Lit;
        return uid;
    }

    [TestCase("TowelColorWhite", 90)]
    [TestCase("MaterialCloth1", 90)]
    [TestCase("Carpet", 240)]
    [TestCase("MaterialWoodPlank1", 300)]
    [TestCase("ClothingUniformJumpsuitColorGrey", 90)]
    [TestCase("CarpetChapel", 240)]
    [TestCase("CarpetCard", 240)]
    [TestCase("WoodenBench", 300)]
    [TestCase("TableCarpet", 300)]
    [TestCase("TableFancyBlack", 300)]
    [TestCase("TableCounterWood", 300)]
    [TestCase("CounterWoodFrame", 300)]
    public async Task CigaretteIgnitesAfterMaterialDelay(string prototype, int seconds)
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel(prototype);
            Cigarette();
            for (var i = 0; i < seconds - 1; i++)
                _fuel.Update(1f);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
            _fuel.Update(1f);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.True);
        });
    }

    [TestCase("CounterMetalFrame")]
    [TestCase("TableFrame")]
    public async Task MetalFramesDoNotInheritSolidFuel(string prototype)
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel(prototype);
            Cigarette();
            for (var i = 0; i < 310; i++)
                _fuel.Update(1f);
            Assert.That(SEntMan.HasComponent<SolidFuelComponent>(uid), Is.False);
            Assert.That(SEntMan.TryGetComponent<FlammableComponent>(uid, out var fire) && fire.OnFire, Is.False);
        });
    }

    [Test]
    public async Task BurningAppearanceReachesClientAndClearsAfterExtinguishing()
    {
        EntityUid target = default;
        NetEntity netTarget = default;
        await Server.WaitAssertion(() =>
        {
            target = SpawnFuel("CarpetChapel");
            netTarget = SEntMan.GetNetEntity(target);
            _fire.Ignite(target, target);
        });
        await Pair.RunSeconds(1);
        await Client.WaitAssertion(() =>
        {
            var uid = CEntMan.GetEntity(netTarget);
            var appearance = CEntMan.System<SharedAppearanceSystem>();
            Assert.That(appearance.TryGetData<bool>(uid, FireVisuals.OnFire, out var burning), Is.True);
            Assert.That(burning, Is.True);
        });
        await Server.WaitAssertion(() => _fire.Extinguish(target));
        await Pair.RunSeconds(1);
        await Client.WaitAssertion(() =>
        {
            var uid = CEntMan.GetEntity(netTarget);
            var appearance = CEntMan.System<SharedAppearanceSystem>();
            Assert.That(appearance.TryGetData<bool>(uid, FireVisuals.OnFire, out var burning), Is.True);
            Assert.That(burning, Is.False);
        });
    }

    [Test]
    public async Task RemovingSourceCoolsMaterial()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel("TowelColorWhite");
            var source = Cigarette();
            for (var i = 0; i < 60; i++)
                _fuel.Update(1f);
            SEntMan.DeleteEntity(source);
            for (var i = 0; i < 40; i++)
                _fuel.Update(1f);
            Assert.That(SEntMan.GetComponent<SolidFuelComponent>(uid).Exposure, Is.Zero);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
        });
    }

    [Test]
    public async Task OxygenRequiredAndLossExtinguishes()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel("TowelColorWhite");
            var source = Cigarette();
            var air = SAtmos.GetContainingMixture(uid)!;
            air.SetMoles(Gas.Oxygen, 0);
            for (var i = 0; i < 100; i++)
                _fuel.Update(1f);
            _fire.Ignite(uid, source);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
            Assert.That(SEntMan.GetComponent<SolidFuelComponent>(uid).Exposure, Is.Zero);
            air.SetMoles(Gas.Oxygen, 20);
            _fire.Ignite(uid, source);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.True);
            air.SetMoles(Gas.Oxygen, 0);
            _fuel.Update(1f);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
        });
    }

    [Test]
    public async Task ExtinguisherReactionStopsBurningAndBlocksRelighting()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel("TowelColorWhite");
            var source = Cigarette();
            _fire.Ignite(uid, source);
            SEntMan.System<ReactiveSystem>().DoEntityReaction(uid, new Solution("Water", 5), ReactionMethod.Touch);
            var fire = SEntMan.GetComponent<FlammableComponent>(uid);
            Assert.That(fire.OnFire, Is.False);
            Assert.That(fire.FireStacks, Is.Negative);
            _fire.Ignite(uid, source);
            _fuel.Update(1f);
            Assert.That(fire.OnFire, Is.False);
            Assert.That(SEntMan.GetComponent<SolidFuelComponent>(uid).Exposure, Is.Zero);
        });
    }

    [Test]
    public async Task AbsorbedWaterPreventsIgnition()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel("TowelColorWhite");
            Cigarette();
            Assert.That(_solutions.TryGetSolution(uid, "absorbed", out var solution, out _), Is.True);
            _solutions.AddSolution(solution!.Value, new Solution("Water", 5));
            for (var i = 0; i < 100; i++)
                _fuel.Update(1f);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
            Assert.That(SEntMan.GetComponent<SolidFuelComponent>(uid).Exposure, Is.Zero);
        });
    }

    [Test]
    public async Task EmergencyToggleExtinguishesExistingFire()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel("TowelColorWhite");
            var source = Cigarette();
            _fire.Ignite(uid, source);
            try
            {
                _config.SetCVar(CCVars.SolidFuelEnabled, false);
                _fuel.Update(1f);
                Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
                _fire.Ignite(uid, source);
                Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
            }
            finally
            {
                _config.SetCVar(CCVars.SolidFuelEnabled, true);
            }
        });
    }

    [TestCase("Lighter", 9)]
    [TestCase("Torch", 3)]
    public async Task StrongerSourcesIgniteFaster(string prototype, int seconds)
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel("TowelColorWhite");
            var source = SpawnFuel(prototype);
            SEntMan.System<SharedIgnitionSourceSystem>().SetIgnited(source);
            for (var i = 0; i < seconds - 1; i++)
                _fuel.Update(1f);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
            _fuel.Update(1f);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.True);
        });
    }

    [Test]
    public async Task BurnoutLeavesAsh()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel("TowelColorWhite");
            var source = Cigarette();
            _fire.Ignite(uid, source);
            for (var i = 0; i < 59; i++)
                _fuel.Update(1f);
            Assert.That(SEntMan.IsQueuedForDeletion(uid), Is.False);
            _fuel.Update(1f);
            Assert.That(SEntMan.IsQueuedForDeletion(uid), Is.True);
            Assert.That(CountAsh(), Is.GreaterThan(0));
        });
    }

    private int CountAsh()
    {
        var count = 0;
        var query = SEntMan.EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var meta))
        {
            if (meta.EntityPrototype?.ID == "Ash")
                count++;
        }
        return count;
    }

    [Test]
    public async Task SprayTileReactionExtinguishesNonCollidingCarpet()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel("Carpet");
            var source = Cigarette();
            _fire.Ignite(uid, source);
            var tile = SEntMan.System<TurfSystem>().GetTileRef(SEntMan.GetComponent<TransformComponent>(uid).Coordinates)!.Value;
            new ExtinguishTileReaction().TileReact(tile, Server.ResolveDependency<IPrototypeManager>().Index(Water), 5, SEntMan, null);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).FireStacks, Is.Negative);
            _fuel.Update(1f);
            Assert.That(SEntMan.GetComponent<SolidFuelComponent>(uid).Exposure, Is.Zero);
        });
    }

    [TestCase("FloorWood", 300, 120)]
    [TestCase("FloorArcadeBlue", 240, 90)]
    [TestCase("FloorArcadeRed", 240, 90)]
    [TestCase("FloorBoxing", 240, 90)]
    [TestCase("FloorGym", 240, 90)]
    public async Task FloorBurnsToUnderlyingTileAndAsh(string prototype, int ignitionTime, int burnTime)
    {
        await Server.WaitAssertion(() =>
        {
            var source = Cigarette();
            var maps = SEntMan.System<SharedMapSystem>();
            var wood = Server.ResolveDependency<IPrototypeManager>().Index<ContentTileDefinition>(prototype);
            var underlying = Server.ResolveDependency<IPrototypeManager>().Index(wood.BaseTurf!.Value);
            maps.SetTile(MapData.Grid, Vector2i.Zero, new Tile(wood.TileId));
            for (var i = 0; i < ignitionTime; i++)
                _fuel.Update(1f);
            EntityUid? floor = null;
            var query = SEntMan.EntityQueryEnumerator<SolidFuelComponent, FlammableComponent>();
            while (query.MoveNext(out var uid, out var fuel, out var fire))
            {
                if (fuel.TileType == wood.ID && fire.OnFire)
                    floor = uid;
            }
            Assert.That(floor, Is.Not.Null);
            for (var i = 0; i < burnTime; i++)
                _fuel.Update(1f);
            var tile = SEntMan.System<TurfSystem>().GetTileRef(SEntMan.GetComponent<TransformComponent>(source).Coordinates)!.Value;
            Assert.That(tile.Tile.TypeId, Is.EqualTo(underlying.TileId));
            Assert.That(CountAsh(), Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task ContainedCigaretteDoesNotHeatNearbyObjects()
    {
        await Server.WaitAssertion(() =>
        {
            var uid = SpawnFuel("TowelColorWhite");
            var source = Cigarette();
            var box = SpawnFuel("BoxCardboard");
            var containers = SEntMan.System<SharedContainerSystem>();
            var container = containers.EnsureContainer<Container>(box, "test-fuel-container");
            Assert.That(containers.Insert(source, container), Is.True);
            for (var i = 0; i < 100; i++)
                _fuel.Update(1f);
            Assert.That(SEntMan.GetComponent<SolidFuelComponent>(uid).Exposure, Is.Zero);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(uid).OnFire, Is.False);
        });
    }

    [Test]
    public async Task SpreadToggleStopsPropagation()
    {
        await Server.WaitAssertion(() =>
        {
            var source = SpawnFuel("Carpet");
            var target = SpawnFuel("TowelColorWhite");
            _fire.Ignite(source, source);
            try
            {
                _config.SetCVar(CCVars.SolidFuelSpread, false);
                for (var i = 0; i < 10; i++)
                    _fuel.Update(1f);
                Assert.That(SEntMan.GetComponent<SolidFuelComponent>(target).Exposure, Is.Zero);
                _config.SetCVar(CCVars.SolidFuelSpread, true);
                for (var i = 0; i < 5; i++)
                    _fuel.Update(1f);
                Assert.That(SEntMan.GetComponent<FlammableComponent>(target).OnFire, Is.True);
            }
            finally
            {
                _config.SetCVar(CCVars.SolidFuelSpread, true);
            }
        });
    }

    [Test]
    public async Task HeldLighterRequiresSustainedInteraction()
    {
        EntityUid target = default;
        await Server.WaitAssertion(() =>
        {
            target = SpawnFuel("TowelColorWhite");
            var user = SpawnFuel("MobHuman");
            var lighter = SpawnFuel("Lighter");
            var hands = SEntMan.GetComponent<HandsComponent>(user);
            Assert.That(SEntMan.System<SharedHandsSystem>().TryPickup(user, lighter, hands.ActiveHandId!), Is.True);
            SEntMan.System<SharedIgnitionSourceSystem>().SetIgnited(lighter);
            Assert.That(SEntMan.System<SharedInteractionSystem>().InteractUsing(user, lighter, target,
                SEntMan.GetComponent<TransformComponent>(target).Coordinates), Is.True);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(target).OnFire, Is.False);
        });
        await Pair.RunSeconds(3);
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<FlammableComponent>(target).OnFire, Is.False));
        await Pair.RunSeconds(12);
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<FlammableComponent>(target).OnFire, Is.True));
    }

    [Test]
    public async Task IncendiaryMeleeCannotEraseWetness()
    {
        await Server.WaitAssertion(() =>
        {
            var target = SpawnFuel("TowelColorWhite");
            var weapon = SpawnFuel("Lighter");
            SEntMan.EnsureComponent<IgniteOnMeleeHitComponent>(weapon).FireStacks = 10;
            SEntMan.System<ReactiveSystem>().DoEntityReaction(target, new Solution("Water", 5), ReactionMethod.Touch);
            var hit = new MeleeHitEvent(new List<EntityUid> { target }, weapon, weapon, new DamageSpecifier(), null);
            SEntMan.EventBus.RaiseLocalEvent(weapon, hit);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(target).FireStacks, Is.Positive);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(target).OnFire, Is.False);
            Assert.That(SEntMan.GetComponent<SolidFuelComponent>(target).WetTime, Is.Positive);
        });
    }

    [Test]
    public async Task TileReactionDoesNotDoubleWetCollidableObjects()
    {
        await Server.WaitAssertion(() =>
        {
            var target = SpawnFuel("TowelColorWhite");
            SEntMan.System<ReactiveSystem>().DoEntityReaction(target, new Solution("Water", 5), ReactionMethod.Touch);
            var before = SEntMan.GetComponent<FlammableComponent>(target).FireStacks;
            var wetBefore = SEntMan.GetComponent<SolidFuelComponent>(target).WetTime;
            var tile = SEntMan.System<TurfSystem>().GetTileRef(SEntMan.GetComponent<TransformComponent>(target).Coordinates)!.Value;
            new ExtinguishTileReaction().TileReact(tile, Server.ResolveDependency<IPrototypeManager>().Index(Water), 5, SEntMan, null);
            Assert.That(SEntMan.GetComponent<FlammableComponent>(target).FireStacks, Is.EqualTo(before));
            Assert.That(SEntMan.GetComponent<SolidFuelComponent>(target).WetTime, Is.EqualTo(wetBefore));
        });
    }

    [TestCase("FloorWood")]
    [TestCase("FloorWoodLarge")]
    public async Task ExistingFloorFuelIsReused(string prototype)
    {
        await Server.WaitAssertion(() =>
        {
            Cigarette();
            ProtoId<ContentTileDefinition> tileId = prototype;
            var wood = Server.ResolveDependency<IPrototypeManager>().Index(tileId);
            SEntMan.System<SharedMapSystem>().SetTile(MapData.Grid, Vector2i.Zero, new Tile(wood.TileId));
            // Represents a serialized floor fire loaded before the first heat-source update.
            var existing = SpawnFuel("SolidFuelFloorWood");
            SEntMan.GetComponent<SolidFuelComponent>(existing).TileType = wood.ID;
            SEntMan.GetComponent<SolidFuelComponent>(existing).Exposure = 20;
            _fuel.Update(1f);
            var count = 0;
            var query = SEntMan.EntityQueryEnumerator<SolidFuelComponent>();
            while (query.MoveNext(out var fuel))
            {
                if (fuel.TileType == wood.ID)
                    count++;
            }
            Assert.That(count, Is.EqualTo(1));
            // The system can have a partial second accumulated from normal map startup ticks.
            Assert.That(SEntMan.GetComponent<SolidFuelComponent>(existing).Exposure, Is.InRange(21f, 22f));
        });
    }
}
