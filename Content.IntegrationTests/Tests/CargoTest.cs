using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Nutrition.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Containers;
using Content.Shared.EntityTable;
using Content.Shared.Mobs.Components;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class CargoTest : GameTest
{
    private static readonly HashSet<ProtoId<CargoProductPrototype>> Ignored = [];

    [SidedDependency(Side.Server)]
    private readonly EntityTableSystem _sTableSystem = null!;

    [SidedDependency(Side.Server)]
    private readonly IComponentFactory _sCompFact = null!;

    [SidedDependency(Side.Server)]
    private readonly PricingSystem _sPricing = null!;

    [SidedDependency(Side.Server)]
    private readonly CargoSystem _sCargo = null!;

    [Test]
    public async Task NoCargoOrderArbitrage()
    {
        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in SProtoMan.EnumeratePrototypes<CargoProductPrototype>())
                {
                    if (Ignored.Contains(proto.ID))
                        continue;

                    if (proto.SpawnList.Count == 0)
                    {
                        Assert.Fail($"CargoProductPrototype {proto.ID} has no products defined.");
                        continue;
                    }
                    List<CargoOrderItemData> basket = [new CargoOrderItemData(proto.ID, 1)];
                    var entProto = SProtoMan.Index<EntityPrototype>(proto.SpawnList.First());
                    double price = 0;
                    if (entProto.TryGetComponent<EntityTableContainerFillComponent>(out var fill, _sCompFact))
                    {
                        var averageSpawns = _sTableSystem.AverageSpawns(fill.Containers.First().Value);
                        // Randomness will lead to non interger expected values, if all the expected values are intergers then we skip
                        // Compares against epsilon incase of any floating point stuff
                        // Edge case of expected value being integers while still random
                        // This might be, crate spawns 2 items from list of 2, each would have ev of 1
                        if (!averageSpawns.All(item => Math.Abs(item.Item2 % 1) <= Double.Epsilon * 100))
                        {
                            foreach (var item in averageSpawns)
                            {
                                var ent = SSpawnAtPosition(item.spawn, coordinates);
                                price += _sPricing.GetPrice(ent) * item.Item2;
                                SEntMan.DeleteEntity(ent);
                            }
                            // Price of container is not included right now
                            Assert.That(price, Is.AtMost(_sCargo.GetBasketTotalCost(basket)),
                                $"Found arbitrage on {proto.ID} cargo product!  Cost is {_sCargo.GetBasketTotalCost(basket)} but mean sell price is {price}!");
                            continue;
                        }
                    }
                    var containers = _sCargo.PackBasketIntoContainers(ref basket);
                    if (containers.Count() != 1)
                    {
                        Assert.Fail($"CargoProductPrototype {proto.ID} spawns packs into multiple containers.");
                        continue;
                    }
                    if (!_sCargo.SpawnContainer(containers.First(), coordinates, out var containerEntity))
                        Assert.Fail($"CargoProductPrototype {proto.ID} could not spawn.");
                    price += _sPricing.GetPrice(containerEntity);
                    Assert.That(price, Is.AtMost(_sCargo.GetBasketTotalCost(basket)),
                        $"Found arbitrage on {proto.ID} cargo product! Cost is {_sCargo.GetBasketTotalCost(basket)} but sell price is {price}!");
                    SEntMan.DeleteEntity(containerEntity);
                }
            });
        });
    }

    [Test]
    public async Task NoCargoBountyArbitrageTest()
    {
        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in SProtoMan.EnumeratePrototypes<CargoProductPrototype>())
                {
                    if (proto.SpawnList.Count == 0)
                    {
                        Assert.Fail($"CargoProductPrototype {proto.ID} has no products defined.");
                        continue;
                    }

                    List<CargoOrderItemData> basket = [new CargoOrderItemData(proto.ID, 10)];
                    var containers = _sCargo.PackBasketIntoContainers(ref basket);
                    if (!_sCargo.SpawnContainer(containers.First(), coordinates, out var containerEntity))
                        Assert.Fail($"CargoProductPrototype {proto.ID} could not spawn.");

                    foreach (var bounty in SProtoMan.EnumeratePrototypes<CargoBountyPrototype>())
                    {
                        if (_sCargo.IsBountyComplete(containerEntity, bounty))
                        {
                            basket.First().Quantity = bounty.Entries.First().Amount;
                            basket.First().NumOrdered = 0;
                            containers = _sCargo.PackBasketIntoContainers(ref basket);
                            if (!_sCargo.SpawnContainer(containers.First(), coordinates, out var containerEntity1))
                                Assert.Fail($"CargoProductPrototype {proto.ID} could not spawn.");
                            var cost = _sCargo.GetBasketCost(basket) + _sCargo.GetContainersCost(containers);
                            if (_sCargo.IsBountyComplete(containerEntity1, bounty))
                            {
                                Assert.That(cost, Is.GreaterThanOrEqualTo(bounty.Reward),
                                    $"Found arbitrage on {bounty.ID} cargo bounty! Product {proto.ID} costs {cost} when buying {basket.First().Quantity} " +
                                    $"but fulfills bounty {bounty.ID} with reward {bounty.Reward}!");
                            }
                            SEntMan.DeleteEntity(containerEntity1);
                        }
                    }
                    SEntMan.DeleteEntity(containerEntity);
                }
            });
        });
    }

    [Test]
    public async Task NoStaticPriceAndStackPrice()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                var protoIds = SProtoMan
                    .EnumeratePrototypes<EntityPrototype>()
                    .Where(p => !p.Abstract)
                    .Where(p => !Pair.IsTestPrototype(p))
                    .Where(p => p.Components.ContainsKey("StaticPrice"));

                foreach (var proto in protoIds)
                {
                    // Sanity check
                    Assert.That(
                        proto.TryGetComponent<StaticPriceComponent>(out var staticPriceComp, _sCompFact),
                        Is.True
                    );

                    if (
                        proto.TryGetComponent<StackPriceComponent>(out var stackPriceComp, _sCompFact)
                        && stackPriceComp.Price > 0
                    )
                    {
                        Assert.That(
                            staticPriceComp.Price,
                            Is.EqualTo(0),
                            $"The prototype {proto} has a StackPriceComponent and StaticPriceComponent whose values are not compatible with each other."
                        );
                    }

                    if (proto.HasComponent<StackComponent>(_sCompFact))
                    {
                        Assert.That(
                            staticPriceComp.Price,
                            Is.EqualTo(0),
                            $"The prototype {proto} has a StackComponent and StaticPriceComponent whose values are not compatible with each other."
                        );
                    }
                }
            });
        });
    }

    /// <summary>
    /// Tests to see if any items that are valid for cargo bounties can be sliced into items that
    /// are also valid for the same bounty entry.
    /// </summary>
    [Test]
    public async Task NoSliceableBountyArbitrageTest()
    {
        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                var bounties = SProtoMan.EnumeratePrototypes<CargoBountyPrototype>().ToList();
                var sliceableEntityProtos = SProtoMan
                    .EnumeratePrototypes<EntityPrototype>()
                    .Where(p => !p.Abstract)
                    .Where(p => !Pair.IsTestPrototype(p))
                    .Where(p => p.TryGetComponent<SliceableFoodComponent>(out _, _sCompFact))
                    .Select(p => p.ID);

                foreach (var proto in sliceableEntityProtos)
                {
                    var ent = SSpawnAtPosition(proto, coordinates);
                    var sliceable = SEntMan.GetComponent<SliceableFoodComponent>(ent);

                    // Check each bounty
                    foreach (var bounty in bounties)
                    {
                        // Check each entry in the bounty
                        foreach (var entry in bounty.Entries)
                        {
                            // See if the entity counts as part of this bounty entry
                            if (!_sCargo.IsValidBountyEntry(ent, entry))
                                continue;

                            // Spawn a slice
                            var slice = SSpawnAtPosition(sliceable.Slice, coordinates);

                            // See if the slice also counts for this bounty entry
                            if (!_sCargo.IsValidBountyEntry(slice, entry))
                            {
                                SEntMan.DeleteEntity(slice);
                                continue;
                            }

                            SEntMan.DeleteEntity(slice);

                            // If for some reason it can only make one slice, that's okay, I guess
                            Assert.That(
                                sliceable.TotalCount,
                                Is.EqualTo(1),
                                $"{proto} counts as part of cargo bounty {bounty.ID} and slices into {sliceable.TotalCount} slices which count for the same bounty!"
                            );
                        }
                    }

                    SEntMan.DeleteEntity(ent);
                }
            });
        });
    }

    [TestPrototypes]
    private const string StackProto =
        @"
- type: stack
  id: StackProto
  name: stack-steel
  spawn: StackEnt

- type: entity
  id: StackEnt
  components:
  - type: StackPrice
    price: 20
  - type: Stack
    stackType: StackProto
    count: 5
";

    [Test]
    public async Task StackPrice()
    {
        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;
        await Server.WaitAssertion(() =>
        {
            var ent = SSpawnAtPosition("StackEnt", coordinates);
            var price = _sPricing.GetPrice(ent);
            Assert.That(price, Is.EqualTo(100.0));
        });
    }

    [Test]
    public async Task MobPrice()
    {
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var (proto, comp) in Pair.GetPrototypesWithComponent<MobPriceComponent>())
                {
                    Assert.That(
                        proto.TryGetComponent<MobStateComponent>(out _, _sCompFact),
                        $"Found MobPriceComponent on {proto.ID}, but no MobStateComponent!"
                    );
                }
            });
        });
    }
}
