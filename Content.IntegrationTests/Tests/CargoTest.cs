using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests;

[TestFixture]
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
    private readonly MapLoaderSystem _sMapLoader = null!;

    [SidedDependency(Side.Server)]
    private readonly TransformSystem _sTransform = null!;

    [SidedDependency(Side.Server)]
    private StationSystem _sStation = default!;

    [SidedDependency(Side.Server)]
    private EntityLookupSystem _sLookup = default!;

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

                    Assert.That(
                        price,
                        Is.AtMost(proto.Cost),
                        $"Found arbitrage on {proto.ID} cargo product! Cost is {proto.Cost} but sell is {price}!"
                    );
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
            using (Assert.EnterMultipleScope())
            {
                foreach (var (proto, comp) in Pair.GetPrototypesWithComponent<MobPriceComponent>())
                {
                    Assert.That(
                        proto.TryGetComponent<MobStateComponent>(out _, _sCompFact),
                        $"Found MobPriceComponent on {proto.ID}, but no MobStateComponent!"
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

        ResPath atsPath = new("/Maps/Shuttles/trading_outpost.yml");
        string stationId = "StandardNanotrasenStation";
        string consoleId = "ComputerCargoOrders";

        await Pair.Server.WaitAssertion(() =>
        {
            var iDCard = SSpawnAtPosition("CaptainIDCard", coordinates);
            var station = SSpawnAtPosition(stationId, coordinates);
            _sStation.AddGridToStation(station, Pair.TestMap!.Grid);
            var grid = SetupATS(atsPath, Pair.TestMap!.MapId, station);
            var console = SetupConsole(consoleId, coordinates);
            Assert.That(_sStation.GetOwningStation(console).HasValue, Is.True, "Console setup failed");
            Assert.That(
                STryComp<StationCargoOrderDatabaseComponent>(
                    _sStation.GetOwningStation(console),
                    out var orderDatabase
                ),
                Is.True,
                "Station setup failed"
            );
            var allProducts = _sCargo.GetAvailableProducts(console);
            Assert.That(allProducts.Count(), Is.GreaterThan(0), "No available products");
            var random = new Random().Next(0, allProducts.Count());
            var proto = allProducts.ElementAt(random);
            var entProto = SProtoMan.Index(proto);
            var spawnCount = 10;
            var addOrderEvent = new CargoConsoleAddOrderMessage("", "", proto, spawnCount) { Actor = iDCard };
            SEntMan.EventBus.RaiseLocalEvent(console, addOrderEvent);
            Assert.That(
                orderDatabase.Orders.Values.Sum(v => v.Count()),
                Is.GreaterThan(0),
                $"Order for {proto} was not Added"
            );
            var mostRecentOrder = orderDatabase.Orders[(ProtoId<CargoAccountPrototype>)"Cargo"].LastOrDefault();
            Assert.That(mostRecentOrder.OrderQuantity, Is.EqualTo(spawnCount), "Wrong number of items requested");
            _sCargo.UpdateBankAccount(station, 100000, mostRecentOrder.Account);
            var approveOrderEvent = new CargoConsoleApproveOrderMessage(mostRecentOrder.OrderId) { Actor = iDCard };
            SEntMan.EventBus.RaiseLocalEvent(console, approveOrderEvent);
            Assert.That(
                orderDatabase.Orders.Values.Sum(v => v.Count()),
                Is.EqualTo(0),
                $"Order was {proto} not removed"
            );
            var intersecting = new HashSet<EntityUid>();
            foreach (var pallet in _sCargo.GetCargoPallets(grid.Value.Owner, BuySellType.Buy))
            {
                var aabb = _sLookup.GetAABBNoContainer(
                    pallet.Entity,
                    pallet.PalletXform.LocalPosition,
                    pallet.PalletXform.LocalRotation
                );
                _sLookup.GetLocalEntitiesIntersecting(grid.Value.Owner, aabb, intersecting, LookupFlags.Dynamic);
            }
            var count = 0;
            foreach (var intersectingEnt in intersecting)
            {
                var metaData = SComp<MetaDataComponent>(intersectingEnt);
                if (metaData.EntityPrototype == entProto.Product)
                    count++;
                else
                    Assert.Fail("Spawned wrong entity");
            }
            Assert.That(count, Is.EqualTo(spawnCount), "Wrong number of spawned entities");
            foreach (var intersectingEnt in intersecting)
                SDeleteNow(intersectingEnt);
        });
    }

    private Entity<MapGridComponent>? SetupATS(ResPath atsPath, MapId map, EntityUid station)
    {
        Assert.That(
            _sMapLoader.TryLoadGrid(map, atsPath, out var grid, offset: new Vector2(200, 200)),
            "Failed to Spawn ATS"
        );
        SEntMan.AddComponent<TradeStationComponent>(grid.Value.Owner);
        _sStation.AddGridToStation(station, grid.Value.Owner, grid.Value.Comp);
        return grid;
    }

    private Entity<CargoOrderConsoleComponent> SetupConsole(string protoId, EntityCoordinates coordinates)
    {
        var console = SSpawnAtPosition(protoId, coordinates);
        var consoleComponent = SComp<CargoOrderConsoleComponent>(console);
        return (console, consoleComponent);
    }
}
