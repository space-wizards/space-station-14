using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class CargoTest
{
    private static readonly HashSet<ProtoId<CargoProductPrototype>> Ignored =
    [
        // This is ignored because it is explicitly intended to be able to sell for more than it costs.
        new("FunCrateGambling")
    ];

    [Test]
    public async Task NoCargoOrderArbitrage()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        var entManager = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var pricing = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<PricingSystem>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<CargoProductPrototype>())
                {
                    if (Ignored.Contains(proto.ID))
                        continue;

                    var ent = entManager.SpawnEntity(proto.Product, testMap.MapCoords);
                    var price = pricing.GetPrice(ent);

                    Assert.That(price, Is.AtMost(proto.Cost), $"Found arbitrage on {proto.ID} cargo product! Cost is {proto.Cost} but sell is {price}!");
                    entManager.DeleteEntity(ent);
                }
            });
        });

        await pair.CleanReturnAsync();
    }
    [Test]
    public async Task NoCargoBountyArbitrageTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = server.System<SharedMapSystem>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var cargo = entManager.System<CargoSystem>();

        var bounties = protoManager.EnumeratePrototypes<CargoBountyPrototype>().ToList();

        await server.WaitAssertion(() =>
        {
            var mapId = testMap.MapId;

            Assert.Multiple(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<CargoProductPrototype>())
                {
                    var ent = entManager.SpawnEntity(proto.Product, new MapCoordinates(Vector2.Zero, mapId));

                    foreach (var bounty in bounties)
                    {
                        if (cargo.IsBountyComplete(ent, bounty))
                            Assert.That(proto.Cost, Is.GreaterThanOrEqualTo(bounty.Reward), $"Found arbitrage on {bounty.ID} cargo bounty! Product {proto.ID} costs {proto.Cost} but fulfills bounty {bounty.ID} with reward {bounty.Reward}!");
                    }

                    entManager.DeleteEntity(ent);
                }
            });

            mapSystem.DeleteMap(mapId);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NoStaticPriceAndStackPrice()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ProtoMan;
        var compFact = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            var protoIds = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => p.Components.ContainsKey("StaticPrice"))
                .ToList();

            foreach (var proto in protoIds)
            {
                // Sanity check
                Assert.That(proto.TryGetComponent<StaticPriceComponent>(out var staticPriceComp, compFact), Is.True);

                if (proto.TryGetComponent<StackPriceComponent>(out var stackPriceComp, compFact) && stackPriceComp.Price > 0)
                {
                    Assert.That(staticPriceComp.Price, Is.EqualTo(0),
                        $"The prototype {proto} has a StackPriceComponent and StaticPriceComponent whose values are not compatible with each other.");
                }

                if (proto.HasComponent<StackComponent>(compFact))
                {
                    Assert.That(staticPriceComp.Price, Is.EqualTo(0),
                        $"The prototype {proto} has a StackComponent and StaticPriceComponent whose values are not compatible with each other.");
                }
            }
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Tests to see if any items that are valid for cargo bounties can be sliced into items that
    /// are also valid for the same bounty entry.
    /// </summary>
    [Test]
    public async Task NoSliceableBountyArbitrageTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSystem = server.System<SharedMapSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var componentFactory = server.ResolveDependency<IComponentFactory>();
        var whitelist = entManager.System<EntityWhitelistSystem>();
        var cargo = entManager.System<CargoSystem>();
        var sliceableSys = entManager.System<SliceableFoodSystem>();

        var bounties = protoManager.EnumeratePrototypes<CargoBountyPrototype>().ToList();

        await server.WaitAssertion(() =>
        {
            var mapId = testMap.MapId;
            var grid = mapManager.CreateGridEntity(mapId);
            var coord = new EntityCoordinates(grid.Owner, 0, 0);

            var sliceableEntityProtos = protoManager.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => p.TryGetComponent<SliceableFoodComponent>(out _, componentFactory))
                .Select(p => p.ID)
                .ToList();

            foreach (var proto in sliceableEntityProtos)
            {
                var ent = entManager.SpawnEntity(proto, coord);
                var sliceable = entManager.GetComponent<SliceableFoodComponent>(ent);

                // Check each bounty
                foreach (var bounty in bounties)
                {
                    // Check each entry in the bounty
                    foreach (var entry in bounty.Entries)
                    {
                        // See if the entity counts as part of this bounty entry
                        if (!cargo.IsValidBountyEntry(ent, entry))
                            continue;

                        // Spawn a slice
                        var slice = entManager.SpawnEntity(sliceable.Slice, coord);

                        // See if the slice also counts for this bounty entry
                        if (!cargo.IsValidBountyEntry(slice, entry))
                        {
                            entManager.DeleteEntity(slice);
                            continue;
                        }

                        entManager.DeleteEntity(slice);

                        // If for some reason it can only make one slice, that's okay, I guess
                        Assert.That(sliceable.TotalCount, Is.EqualTo(1), $"{proto} counts as part of cargo bounty {bounty.ID} and slices into {sliceable.TotalCount} slices which count for the same bounty!");
                    }
                }

                entManager.DeleteEntity(ent);
            }
            mapSystem.DeleteMap(mapId);
        });

        await pair.CleanReturnAsync();
    }

    [TestPrototypes]
    private const string StackProto = @"
- type: entity
  id: A

- type: stack
  id: StackProto
  spawn: A

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
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entManager = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var priceSystem = entManager.System<PricingSystem>();

            var ent = entManager.SpawnEntity("StackEnt", MapCoordinates.Nullspace);
            var price = priceSystem.GetPrice(ent);
            Assert.That(price, Is.EqualTo(100.0));
        });

        await pair.CleanReturnAsync();
    }
}
