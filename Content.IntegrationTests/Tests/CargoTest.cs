using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
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

[TestFixture]
public sealed class CargoTest : GameTest
{
    /// <summary>
    /// <see cref="NoCargoOrderArbitrage"/> will ignore all <see cref="CargoProductPrototype"/>s listed here.
    /// </summary>
    private static readonly HashSet<ProtoId<CargoProductPrototype>> Ignored =
    [

    ];

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
            using (Assert.EnterMultipleScope())
            {
                foreach (var proto in SProtoMan.EnumeratePrototypes<CargoProductPrototype>())
                {
                    if (Ignored.Contains(proto.ID))
                        continue;
                    var entProto = SProtoMan.Index<EntityPrototype>(proto.Product);
                    double price = 0;
                    EntityUid ent;
                    if (entProto.TryGetComponent<EntityTableContainerFillComponent>(out var fill, _sCompFact))
                    {
                        var averageSpawns = _sTableSystem.AverageSpawns(fill.Containers.First().Value);
                        // Randomness will lead to non integer expected values, if all the expected values are integers then we skip
                        // Compares against epsilon in case of any floating point stuff
                        if (!averageSpawns.All(item => Math.Abs(item.Item2 % 1) <= Double.Epsilon * 100))
                        {
                            foreach (var item in averageSpawns)
                            {
                                ent = SSpawnAtPosition(item.spawn, coordinates);
                                price += _sPricing.GetPrice(ent) * item.Item2;
                                SEntMan.DeleteEntity(ent);
                            }
                            // Price of container is not included right now
                            Assert.That(
                                price,
                                Is.AtMost(proto.Cost),
                                $"Found arbitrage on {proto.ID} cargo product!  Cost is {proto.Cost} but mean sell price is {price}!"
                            );
                            // If the price was found using the average price it won't spawn the whole container and skips
                            continue;
                        }
                    }

                    ent = SSpawnAtPosition(proto.Product, coordinates);
                    price += _sPricing.GetPrice(ent);

                    Assert.That(
                        price,
                        Is.AtMost(proto.Cost),
                        $"Found arbitrage on {proto.ID} cargo product! Cost is {proto.Cost} but sell is {price}!"
                    );
                    SEntMan.DeleteEntity(ent);
                }
            };
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
                    var ent = SSpawnAtPosition(proto.Product, coordinates);

                    foreach (var bounty in SProtoMan.EnumeratePrototypes<CargoBountyPrototype>())
                    {
                        if (_sCargo.IsBountyComplete(ent, bounty))
                            Assert.That(
                                proto.Cost,
                                Is.GreaterThanOrEqualTo(bounty.Reward),
                                $"Found arbitrage on {bounty.ID} cargo bounty! Product {proto.ID} costs {proto.Cost} but fulfills bounty {bounty.ID} with reward {bounty.Reward}!"
                            );
                    }

                    SEntMan.DeleteEntity(ent);
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
            var sliceableEntityProtos = SProtoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !Pair.IsTestPrototype(p))
                .Where(p => p.TryGetComponent<ToolRefinableComponent>(out _, _sCompFact))
                .Select(p => p.ID)
                .ToList();

            var bounties = SProtoMan.EnumeratePrototypes<CargoBountyPrototype>();

            foreach (var proto in sliceableEntityProtos)
            {
                var ent = SEntMan.SpawnEntity(proto, coordinates);
                var sliceable = SEntMan.GetComponent<ToolRefinableComponent>(ent);

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

                        var sliceCountByProtoId = EntitySpawnCollection.GetSpawns(sliceable.RefineResult)
                                                                    .GroupBy(x => x)
                                                                    .ToDictionary(x => x.Key, x => x.Count());

                        foreach (var (sliceProtoId, sliceCount) in sliceCountByProtoId)
                        {
                            var slice = SEntMan.SpawnEntity(sliceProtoId, coordinates);

                            // See if the slice also counts for this bounty entry
                            if (!_sCargo.IsValidBountyEntry(slice, entry))
                            {
                                SEntMan.DeleteEntity(slice);
                                continue;
                            }

                            SEntMan.DeleteEntity(slice);

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
