using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.EntityTable;
using Content.Shared.Mobs.Components;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests;

public sealed class CargoTest : GameTest
{
    /// <summary>
    /// <see cref="NoCargoOrderArbitrage"/> will ignore all <see cref="CargoProductPrototype"/>s listed here.
    /// </summary>
    private static readonly HashSet<ProtoId<CargoProductPrototype>> Ignored =
    [
        // This is ignored because it is explicitly intended to be able to sell for more than it costs.
        new("FunCrateGambling"),
    ];

    [SidedDependency(Side.Server)]
    private readonly IComponentFactory _sCompFact = null!;

    [SidedDependency(Side.Server)]
    private readonly PricingSystem _sPricing = null!;

    [SidedDependency(Side.Server)]
    private readonly CargoSystem _sCargo = null!;

    [SidedDependency(Side.Server)]
    private readonly EntityTableSystem _sTableSystem = null!;

    [SidedDependency(Side.Server)]
    private readonly MapLoaderSystem _sMapLoader = null!;

    [SidedDependency(Side.Server)]
    private readonly StationSystem _sStation = null!;

    [SidedDependency(Side.Server)]
    private readonly EntityLookupSystem _sLookup = null!;

    [Test]
    public async Task NoCargoOrderArbitrage()
    {
        var pair = Pair;
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
                    SDeleteNow(ent);
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
            using (Assert.EnterMultipleScope())
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

                    SDeleteNow(ent);
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
                        proto.TryComp<StackPriceComponent>(out var stackPriceComp, _sCompFact)
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

                SDeleteNow(ent);
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
                        proto.TryComp<MobStateComponent>(out _, _sCompFact),
                        $"Found {nameof(MobPriceComponent)} on {proto.ID}, but no {nameof(MobStateComponent)}!"
                    );
                }
            }
        });
    }

    [Test]
    public async Task CargoOrdersFromConsoleSpawn()
    {
        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        await Pair.Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                // Spawn id so that it has permission to accept order
                var iDCard = SSpawnAtPosition("CaptainIDCard", coordinates);
                // Spawn station entity for station bank account and order database
                var station = SetupStation("StandardNanotrasenStation", coordinates);
                // Spawn ATS
                var grid = SetupATS(new("/Maps/Shuttles/trading_outpost.yml"), Pair.TestMap!.MapId, station);
                // Spawn cargo request console
                var console = SetupConsole("ComputerCargoOrders", coordinates);

                // Console is anchored and has station
                Assert.That(_sStation.GetOwningStation(console).HasValue, Is.True, "Console has no owning station");
                // Station has order database
                Assert.That(
                    STryComp<StationCargoOrderDatabaseComponent>(
                        _sStation.GetOwningStation(console),
                        out var orderDatabase
                    ),
                    Is.True,
                    "Station has no cargo order database"
                );
                // No orders in the database yet
                Assert.That(
                    orderDatabase.Orders.Values.Sum(v => v.Count()),
                    Is.EqualTo(0),
                    $"Order database did not start empty"
                );

                // Only get products which this console can request
                var allProducts = _sCargo.GetAvailableProducts(console).ToList();
                Assert.That(allProducts, Is.Not.Empty, "No available cargo products");

                // How many items to ask for in test
                const int spawnCount = 1;
                foreach (var proto in allProducts)
                {
                    var productProto = SProtoMan.Index(proto);
                    var entProto = SProtoMan.Index(productProto.Product);

                    // Place order
                    SEntMan.EventBus.RaiseLocalEvent(
                        console,
                        new CargoConsoleAddOrderMessage("", "", proto, spawnCount) { Actor = iDCard }
                    );

                    // Check order was placed
                    Assert.That(
                        orderDatabase.Orders.Values.Sum(v => v.Count()),
                        Is.GreaterThan(0),
                        $"[{proto}] Order was not added"
                    );

                    // Get last placed  order
                    var order = orderDatabase.Orders[console.Comp.Account].LastOrDefault();
                    Assert.That(
                        order.OrderQuantity,
                        Is.EqualTo(spawnCount),
                        $"[{proto}] Order quantity does not match requested amount"
                    );

                    // Adding money so order is always approved
                    _sCargo.UpdateBankAccount(station, productProto.Cost * spawnCount, order.Account);
                    // Approve order
                    SEntMan.EventBus.RaiseLocalEvent(
                        console,
                        new CargoConsoleApproveOrderMessage(order.OrderId) { Actor = iDCard }
                    );

                    // Check order is removed after approval and approval went through
                    Assert.That(
                        orderDatabase.Orders.Values.Sum(v => v.Count()),
                        Is.EqualTo(0),
                        $"[{proto}] Order was not removed after approval"
                    );

                    // Verify spawned entities on pallets
                    var spawnedEntities = GetEntitiesOnCargoPallets(grid.Owner);
                    var count = 0;
                    var expectedProtos = new List<EntProtoId> { productProto.Product };
                    // Some products spawn inside containers e.g. silver inside parcel
                    if (productProto.Container is { } container)
                        expectedProtos.Add(container.Entity.Id);
                    else if (
                        entProto.TryGetComponent<EntityTableSpawnerComponent>(out var tableSpawnerComponent, _sCompFact)
                    )
                    {
                        expectedProtos = _sTableSystem
                            .ListSpawns(tableSpawnerComponent.Table)
                            .Select(x => x.spawn)
                            .ToList();
                    }
                    foreach (var entity in spawnedEntities)
                    {
                        // Get the prototype of the entity on the cargo pad
                        var spawnedPrototype = (EntProtoId)SComp<MetaDataComponent>(entity).EntityPrototype;

                        // Receipt paper may spawn separately
                        if (spawnedPrototype == orderDatabase.PrinterOutput)
                        {
                            SDeleteNow(entity);
                            continue;
                        }

                        count++;
                        // Check spawned prototype is the same as requested product
                        Assert.That(
                            spawnedPrototype,
                            Is.AnyOf(expectedProtos),
                            $"[{proto}] Spawned entity has wrong prototype"
                        );
                        SDeleteNow(entity);
                    }

                    // Check the amount of items spawned is the same as requested
                    Assert.That(
                        count,
                        Is.EqualTo(spawnCount),
                        $"[{proto}] Expected {spawnCount} spawned entities, got {count}"
                    );
                }
            }
        });
    }

    private EntityUid SetupStation(string protoId, EntityCoordinates coordinates)
    {
        var station = SSpawnAtPosition(protoId, coordinates);
        _sStation.AddGridToStation(station, Pair.TestMap!.Grid);
        return station;
    }

    private Entity<MapGridComponent> SetupATS(ResPath atsPath, MapId map, EntityUid station)
    {
        Assert.That(
            _sMapLoader.TryLoadGrid(map, atsPath, out var grid, offset: new Vector2(200, 200)),
            "Failed to load ATS grid"
        );
        SEntMan.AddComponent<TradeStationComponent>(grid.Value.Owner);
        _sStation.AddGridToStation(station, grid.Value.Owner, grid.Value.Comp);
        return grid.Value;
    }

    private Entity<CargoOrderConsoleComponent> SetupConsole(string protoId, EntityCoordinates coordinates)
    {
        var console = SSpawnAtPosition(protoId, coordinates);
        return (console, SComp<CargoOrderConsoleComponent>(console));
    }

    private IEnumerable<EntityUid> GetEntitiesOnCargoPallets(EntityUid gridOwner)
    {
        var entities = new HashSet<EntityUid>();
        foreach (var pallet in _sCargo.GetCargoPallets(gridOwner, BuySellType.Buy))
        {
            var aabb = _sLookup.GetAABBNoContainer(
                pallet.Entity,
                pallet.PalletXform.LocalPosition,
                pallet.PalletXform.LocalRotation
            );
            _sLookup.GetLocalEntitiesIntersecting(gridOwner, aabb, entities, LookupFlags.Dynamic);
        }
        return entities.Distinct();
    }
}
