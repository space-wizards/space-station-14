using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Containers;
using Content.Shared.EntityTable;
using Content.Shared.Mobs.Components;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

public sealed class CargoTest : GameTest
{
    /// <summary>
    /// <see cref="NoCargoOrderArbitrage"/> will ignore all <see cref="CargoProductPrototype"/>s listed here.
    /// </summary>
    private static readonly HashSet<ProtoId<CargoProductPrototype>> Ignored =
    [

    ];

    [SidedDependency(Side.Server)]
    private readonly IComponentFactory _sCompFact = null!;

    [SidedDependency(Side.Server)]
    private readonly PricingSystem _sPricing = null!;

    [SidedDependency(Side.Server)]
    private readonly CargoSystem _sCargo = null!;

    [SidedDependency(Side.Server)]
    private readonly EntityTableSystem _sTableSystem = null!;

    [Test]
    public async Task NoCargoOrderArbitrage()
    {
        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
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
                                SDeleteNow(ent);
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
            }
        });
    }

    [Test]
    public async Task NoCargoBountyArbitrageTest()
    {
        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
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
                            SDeleteNow(containerEntity1);
                        }
                    }
                    SDeleteNow(containerEntity);
                }
            }
        });
    }

    [Test]
    public async Task NoStaticPriceAndStackPrice()
    {
        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                var protoIds = Pair.GetPrototypesWithComponent<StaticPriceComponent>();

                foreach (var (proto, staticPriceComp) in protoIds)
                {
                    if (
                        proto.TryGetComponent<StackPriceComponent>(out var stackPriceComp, _sCompFact)
                        && stackPriceComp.Price > 0
                    )
                    {
                        Assert.That(
                            staticPriceComp.Price,
                            Is.EqualTo(0),
                            $"The prototype {proto} has a {nameof(StackPriceComponent)} and {nameof(StaticPriceComponent)} whose values are not compatible with each other."
                        );
                    }

                    if (proto.HasComponent<StackComponent>(_sCompFact))
                    {
                        Assert.That(
                            staticPriceComp.Price,
                            Is.EqualTo(0),
                            $"The prototype {proto} has a {nameof(StackComponent)} and {nameof(StaticPriceComponent)} whose values are not compatible with each other."
                        );
                    }
                }
            }
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

        var bounties = SProtoMan.EnumeratePrototypes<CargoBountyPrototype>().ToList();

        await Server.WaitAssertion(() =>
        {
            var sliceableEntityProtos = Pair.GetPrototypesWithComponent<ToolRefinableComponent>();

            foreach (var (proto, sliceable) in sliceableEntityProtos)
            {
                var ent = SSpawnAtPosition(proto.ID, coordinates);

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

                        var sliceCountByProtoId = EntitySpawnCollection
                            .GetSpawns(sliceable.RefineResult)
                            .GroupBy(x => x)
                            .ToDictionary(x => x.Key, x => x.Count());

                        foreach (var (sliceProtoId, sliceCount) in sliceCountByProtoId)
                        {
                            var slice = SSpawnAtPosition(sliceProtoId, coordinates);

                            // See if the slice also counts for this bounty entry
                            if (!_sCargo.IsValidBountyEntry(slice, entry))
                            {
                                SDeleteNow(slice);
                                continue;
                            }

                            SDeleteNow(slice);

                            // If for some reason it can only make one slice, that's okay, I guess
                            Assert.That(
                                sliceCount,
                                Is.EqualTo(1),
                                $"{proto} counts as part of cargo bounty {bounty.ID} "
                                    + $"and slices into {sliceCount} slices which count for the same bounty!"
                            );
                        }
                    }
                }

                SEntMan.DeleteEntity(ent);
            }
        });
    }

    private const string StackEnt = "StackEnt";
    private const string StackCount = "5";
    private const string StackUnitPrice = "20";

    [TestPrototypes]
    private const string StackProto =
        @$"
- type: stack
  id: StackProto
  name: stack-steel
  spawn: {StackEnt}

- type: entity
  id: {StackEnt}
  components:
  - type: StackPrice
    price: {StackUnitPrice}
  - type: Stack
    stackType: StackProto
    count: {StackCount}
";

    [Test]
    public async Task StackPrice()
    {
        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;
        await Server.WaitAssertion(() =>
        {
            var ent = SSpawnAtPosition(StackEnt, coordinates);
            var price = _sPricing.GetPrice(ent);
            Assert.That(price, Is.EqualTo(double.Parse(StackCount) * double.Parse(StackUnitPrice)));
        });
    }

    [Test]
    public async Task MobPrice()
    {
        await Pair.Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var (proto, comp) in Pair.GetPrototypesWithComponent<MobPriceComponent>())
                {
                    Assert.That(
                        proto.TryGetComponent<MobStateComponent>(out _, _sCompFact),
                        $"Found {nameof(MobPriceComponent)} on {proto.ID}, but no {nameof(MobStateComponent)}!"
                    );
                }
            }
        });
    }
}
