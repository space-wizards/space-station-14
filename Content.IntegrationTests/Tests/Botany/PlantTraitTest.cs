#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Chemistry.Components;
using Content.Server.Damage.Components;
using Content.Server.Destructible;
using Content.Server.Gravity;
using Content.Shared.Trigger.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Verbs;
using Robust.Shared.Localization;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits;
using Content.Shared.Damage.Components;
using Content.Shared.Gravity;
using Content.Shared.Slippery;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Botany;

[TestOf(typeof(PlantTraitPrototype))]
public sealed class PlantTraitTest : GameTest
{
    private static readonly ProtoId<PlantTraitPrototype> Slippery = "Slippery";
    private static readonly ProtoId<PlantTraitPrototype> Squash = "Squash";
    private static readonly ProtoId<PlantTraitPrototype> NettleSting = "NettleSting";
    private static readonly ProtoId<PlantTraitPrototype> DeathNettleSting = "DeathNettleSting";

    private static readonly EntProtoId WheatPlants = "WheatPlants";
    private static readonly EntProtoId TomatoPlants = "TomatoPlants";
    private static readonly EntProtoId BananaPlants = "BananaPlants";
    private static readonly EntProtoId NettlePlants = "NettlePlants";
    private static readonly EntProtoId DeathNettlePlants = "DeathNettlePlants";

    private const string SquashJuice = "PlantTraitTestSquashJuice";
    private const string SquashVictim = "PlantTraitTestSquashVictim";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: reagent
  id: {SquashJuice}
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing
  reactiveEffects:
    Acidic:
      methods: [ Touch ]
      effects:
      - !type:HealthChange
        damage:
          types:
            Caustic: 10

- type: entity
  parent: MobHuman
  id: {SquashVictim}
  components:
  - type: Reactive
    groups:
      Acidic: [ Touch ]
";

    /// <summary>
    /// Number of cross-pollinations used to check random trait inheritance.
    /// A 50% event fails to happen in all of them with a chance of 2^-50.
    /// </summary>
    private const int CrossAttempts = 50;

    [SidedDependency(Side.Server)] private readonly BotanySystem _botany = null!;
    [SidedDependency(Side.Server)] private readonly PlantMutationSystem _mutation = null!;
    [SidedDependency(Side.Server)] private readonly ThrowingSystem _throwing = null!;
    [SidedDependency(Side.Server)] private readonly GravitySystem _gravity = null!;
    [SidedDependency(Side.Server)] private readonly SharedMapSystem _map = null!;
    [SidedDependency(Side.Server)] private readonly SharedSolutionContainerSystem _solutionContainer = null!;
    [SidedDependency(Side.Server)] private readonly SharedVerbSystem _verbs = null!;
    [SidedDependency(Side.Server)] private readonly SlipperySystem _slippery = null!;
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damageable = null!;

    [Test]
    [TestOf(typeof(BotanySystem))]
    public async Task SameTraitWorksOnDifferentPlants()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            foreach (var plantProto in new[] { WheatPlants, TomatoPlants })
            {
                var plant = SSpawnAtPosition(plantProto, map.GridCoords);
                Assert.That(_mutation.AddTrait(plant, Slippery), Is.True);

                var produce = HarvestOne(plant, map.GridCoords);
                Assert.That(SEntMan.HasComponent<SlipperyComponent>(produce), Is.True,
                    $"Produce of {plantProto} with the {Slippery} trait is not slippery.");
            }
        });
    }

    [Test]
    [TestOf(typeof(BotanySystem))]
    public async Task HarvestedProduceUsesPlantTraits()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var plant = SSpawnAtPosition(TomatoPlants, map.GridCoords);
            Assert.That(_mutation.HasTrait(plant, Squash), Is.True);
            Assert.That(SEntMan.HasComponent<DestructibleComponent>(HarvestOne(plant, map.GridCoords)), Is.True);

            // A tomato plant that lost its Squash trait grows tomatoes that do not burst,
            // so none of the squash behavior may come from the tomato prototype itself.
            Assert.That(_mutation.RemoveTrait(plant, Squash), Is.True);
            var plainTomato = HarvestOne(plant, map.GridCoords);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(SEntMan.HasComponent<DestructibleComponent>(plainTomato), Is.False);
                Assert.That(SEntMan.HasComponent<DamageOnLandComponent>(plainTomato), Is.False);
                Assert.That(SEntMan.HasComponent<DamageOtherOnHitComponent>(plainTomato), Is.False);
            }
        });
    }

    [Test]
    [TestOf(typeof(BotanySystem))]
    public async Task UnharvestedProduceGetsSpeciesTraits()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var tomato = SSpawnAtPosition("FoodTomato", map.GridCoords);
            Assert.That(SEntMan.HasComponent<DestructibleComponent>(tomato), Is.True);
            Assert.That(SComp<DamageOnHighSpeedImpactComponent>(tomato).MinimumSpeed, Is.EqualTo(0.1f));
            Assert.That(SEntMan.HasComponent<DamageOnLandComponent>(tomato), Is.True);
            Assert.That(SEntMan.HasComponent<DamageOtherOnHitComponent>(tomato), Is.True);

            var nettle = SSpawnAtPosition("Nettle", map.GridCoords);
            Assert.That(SEntMan.HasComponent<MeleeWeaponComponent>(nettle), Is.True);
            Assert.That(SEntMan.HasComponent<MeleeChemicalInjectorComponent>(nettle), Is.True);

            var banana = SSpawnAtPosition("FoodBanana", map.GridCoords);
            Assert.That(SEntMan.HasComponent<SlipperyComponent>(banana), Is.True);
        });
    }

    [Test]
    [TestOf(typeof(PlantMutationSystem))]
    public async Task CrossPollinationTransfersUnprotectedTraits()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var pollen = SSpawnAtPosition(TomatoPlants, map.GridCoords);
            var transferred = false;
            for (var i = 0; i < CrossAttempts && !transferred; i++)
            {
                var target = SSpawnAtPosition(WheatPlants, map.GridCoords);
                _mutation.CrossTraits(pollen, TomatoPlants, target);
                transferred = _mutation.HasTrait(target, Squash);
            }

            Assert.That(transferred, Is.True, $"{Squash} was never passed on in {CrossAttempts} cross-pollinations.");
        });
    }

    [Test]
    [TestOf(typeof(PlantMutationSystem))]
    public async Task CrossPollinationKeepsProtectedTraits()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var nettle = SSpawnAtPosition(NettlePlants, map.GridCoords);
            var wheat = SSpawnAtPosition(WheatPlants, map.GridCoords);

            for (var i = 0; i < CrossAttempts; i++)
            {
                // Pollen from a nettle never passes on the protected sting...
                var target = SSpawnAtPosition(WheatPlants, map.GridCoords);
                _mutation.CrossTraits(nettle, NettlePlants, target);
                Assert.That(_mutation.HasTrait(target, NettleSting), Is.False);

                // ...and a nettle never loses it.
                _mutation.CrossTraits(wheat, WheatPlants, nettle);
                Assert.That(_mutation.HasTrait(nettle, NettleSting), Is.True);
            }
        });
    }

    [Test]
    [TestOf(typeof(PlantMutationSystem))]
    public async Task SpeciesChangeSwapsDefaultTraits()
    {
        var map = await Pair.CreateTestMap();
        EntityUid nettle = default;

        await Server.WaitPost(() =>
        {
            nettle = SSpawnAtPosition(NettlePlants, map.GridCoords);
            _mutation.AddTrait(nettle, Slippery);
            _mutation.SpeciesChange(nettle, DeathNettlePlants);
        });

        await RunTicksSync(1);

        await Server.WaitAssertion(() =>
        {
            var deathNettles = new List<EntityUid>();
            var query = SEntMan.EntityQueryEnumerator<PlantComponent, MetaDataComponent>();
            while (query.MoveNext(out var uid, out _, out var meta))
            {
                if (meta.EntityPrototype?.ID == DeathNettlePlants.Id)
                    deathNettles.Add(uid);
            }

            Assert.That(deathNettles, Has.Count.EqualTo(1));
            var deathNettle = deathNettles[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_mutation.HasTrait(deathNettle, DeathNettleSting), Is.True, "New species default trait missing.");
                Assert.That(_mutation.HasTrait(deathNettle, NettleSting), Is.False, "Old species default trait kept.");
                Assert.That(_mutation.HasTrait(deathNettle, Slippery), Is.True, "Mutated trait lost.");
            }
        });
    }

    [Test]
    [TestOf(typeof(BotanySystem))]
    [TestCase("TomatoPlants")]
    [TestCase("BananaPlants")]
    [TestCase("WheatPlants")]
    public async Task SquashProduceBurstsWhenThrownOntoTheFloor(string plantProto)
    {
        var map = await Pair.CreateTestMap();
        EntityUid produce = default;

        await Server.WaitPost(() =>
        {
            // A strip of floor with gravity, so that thrown produce lands instead of floating away.
            for (var x = -2; x < 12; x++)
            {
                for (var y = -2; y < 3; y++)
                {
                    _map.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, y), map.Tile.Tile);
                }
            }

            SEntMan.EnsureComponent<GravityComponent>(map.Grid.Owner);
            _gravity.EnableGravity(map.Grid.Owner);

            // Keep the plant out of the throw's path, it is solid.
            var plant = SSpawnAtPosition(plantProto, new EntityCoordinates(map.Grid.Owner, -1.5f, -1.5f));
            _mutation.AddTrait(plant, Squash);

            var start = new EntityCoordinates(map.Grid.Owner, 2.5f, 0.5f);
            produce = HarvestOne(plant, start);
            SEntMan.System<SharedTransformSystem>().SetCoordinates(produce, start);
            _throwing.TryThrow(produce, new Vector2(5, 0));
        });

        await RunSeconds(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(produce), Is.True, $"Produce of {plantProto} with {Squash} did not burst on landing.");
        });
    }

    [Test]
    [TestOf(typeof(SplashOnTriggerSystem))]
    public async Task SquashingByHandSplashesTheUser()
    {
        var map = await Pair.CreateTestMap();
        EntityUid produce = default;
        EntityUid victim = default;

        await Server.WaitPost(() =>
        {
            victim = SSpawnAtPosition(SquashVictim, map.GridCoords);
            produce = HarvestSquashJuiceProduce(TomatoPlants, map.GridCoords, slippery: false);

            var verbs = _verbs.GetLocalVerbs(produce, victim, typeof(AlternativeVerb));
            var squashText = Loc.GetString("plant-trait-verb-squash");
            var squash = verbs.Single(verb => verb.Text == squashText);
            // Alt-click uses the first alternative verb. Eating stays on use in hand.
            Assert.That(verbs, Has.Count.GreaterThan(1), "Expected an eat verb next to the squash verb.");
            Assert.That(verbs.First(), Is.EqualTo(squash), "Alt-click does not squash the produce.");

            _verbs.ExecuteVerb(squash, victim, produce);
        });

        await RunTicksSync(5);
        await Server.WaitAssertion(() => AssertSplashedOnlyOnto(produce, victim));
    }

    [Test]
    [TestOf(typeof(SplashOnTriggerSystem))]
    public async Task SlipperySquashProduceSplashesWhoeverSlips()
    {
        var map = await Pair.CreateTestMap();
        EntityUid produce = default;
        EntityUid victim = default;

        await Server.WaitPost(() =>
        {
            victim = SSpawnAtPosition(SquashVictim, map.GridCoords);
            produce = HarvestSquashJuiceProduce(TomatoPlants, map.GridCoords, slippery: true);
            _slippery.TrySlip(produce, SComp<SlipperyComponent>(produce), victim, requiresContact: false);
        });

        await RunTicksSync(5);
        await Server.WaitAssertion(() => AssertSplashedOnlyOnto(produce, victim));
    }

    /// <summary>
    /// Harvests a produce with the Squash trait, filled with juice that hurts whoever it touches.
    /// </summary>
    private EntityUid HarvestSquashJuiceProduce(EntProtoId plantProto, EntityCoordinates coords, bool slippery)
    {
        var plant = SSpawnAtPosition(plantProto, coords);
        _mutation.AddTrait(plant, Squash);
        if (slippery)
            _mutation.AddTrait(plant, Slippery);

        var produce = HarvestOne(plant, coords);
        Assert.That(_solutionContainer.TryGetSolution(produce, "food", out var solution, out _), Is.True);
        _solutionContainer.RemoveAllSolution(solution!.Value);
        _solutionContainer.TryAddReagent(solution.Value, SquashJuice, 10);
        return produce;
    }

    /// <summary>
    /// Checks that a squashed produce burst and that all of its contents went onto the victim, leaving no puddle.
    /// </summary>
    private void AssertSplashedOnlyOnto(EntityUid produce, EntityUid victim)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(SEntMan.Deleted(produce), Is.True, "The produce did not burst.");
            Assert.That(_damageable.GetTotalDamage(victim), Is.GreaterThan(FixedPoint2.Zero), "The victim was not splashed.");
            Assert.That(SEntMan.EntityQuery<PuddleComponent>().Count(), Is.Zero, "Some of the contents were spilled instead of splashed.");
        }
    }

    /// <summary>
    /// Harvests a single produce from a plant and returns it.
    /// </summary>
    private EntityUid HarvestOne(EntityUid plant, EntityCoordinates coords)
    {
        var before = AllProduce();
        _botany.SpawnProduce(new Entity<PlantComponent?, PlantDataComponent?>(plant, null, null), coords, 1);
        var spawned = AllProduce().Except(before).ToList();
        Assert.That(spawned, Has.Count.EqualTo(1));
        return spawned[0];
    }

    private HashSet<EntityUid> AllProduce()
    {
        var produce = new HashSet<EntityUid>();
        var query = SEntMan.EntityQueryEnumerator<ProduceComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            produce.Add(uid);
        }

        return produce;
    }
}
