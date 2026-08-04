using System.Collections.Immutable;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using Content.Server.Database;
using Content.Server.DeadSpace.Arena;
using Content.Server.DeadSpace.Prison.Components;
using Content.Server.DeadSpace.Prison;
using Content.Shared.Cargo.Components;
using Content.Shared.Destructible;
using Content.Shared.Database;
using Content.Server.Stack;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.DeadSpace.Prison;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.DeadSpace.Prison;

[TestFixture]
public sealed class PrisonOreProvenanceTest
{
    [Test]
    public async Task LobbyBanRegistersPrisonerWithoutMovingLobbySession()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true,
        });

        var server = pair.Server;
        var player = pair.Player!;
        var prison = server.System<PrisonSystem>();
        var ticker = server.System<Content.Server.GameTicking.GameTicker>();
        var database = server.ResolveDependency<IServerDbManager>();
        var originalEntity = player.AttachedEntity;

        await server.WaitPost(() =>
        {
            server.CfgMan.SetCVar(CCCCVars.PrisonEnabled, true);
            var mapSystem = server.System<SharedMapSystem>();
            mapSystem.CreateMap(out var mapId);
            server.EntMan.SpawnEntity("SpawnPointPrisoner", new MapCoordinates(Vector2.Zero, mapId));
        });

        Assert.That(ticker.UserHasJoinedGame(player), Is.False, "The test session must still be in the lobby.");
        var now = DateTimeOffset.UtcNow;
        var ban = await AddPrisonBan(database, player.UserId, now, now + TimeSpan.FromHours(1));
        var handled = false;

        await server.WaitPost(() => handled = prison.TrySendToPrison(player, ban));

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(player.AttachedEntity, Is.EqualTo(originalEntity));
            Assert.That(prison.IsUserPrisoner(player.UserId), Is.True);
            Assert.That(server.System<ArenaSystem>().CanJoinArena(player), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PhysicalShipmentUsesLooseOreBelowThresholdAndOpenCrateAtThreshold()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var userId = new NetUserId(Guid.NewGuid());
        EntityUid shuttle = default;

        await server.WaitPost(() =>
        {
            var mapSystem = server.System<SharedMapSystem>();
            mapSystem.CreateMap(out var mapId);
            var grid = server.ResolveDependency<IMapManager>().CreateGridEntity(mapId);
            shuttle = grid.Owner;
            for (var x = 0; x < 3; x++)
                mapSystem.SetTile(grid.Owner, grid.Comp, new Vector2i(x, 0), new Tile(1));
            server.EntMan.EnsureComponent<CargoShuttleComponent>(shuttle);

            var processor = new PrisonOreProcessorComponent
            {
                PointsPerSecond = 10,
                CrateMinimumUnits = 10,
            };
            processor.OreValues["SteelOre"] = 1;

            var prisonOre = server.System<PrisonOreSystem>();
            Assert.That(
                prisonOre.TryCreatePhysicalShipment(
                    new Dictionary<Robust.Shared.Prototypes.ProtoId<StackPrototype>, int> { ["SteelOre"] = 5 },
                    userId,
                    1,
                    processor,
                    out _),
                Is.True);
            Assert.That(
                prisonOre.TryCreatePhysicalShipment(
                    new Dictionary<Robust.Shared.Prototypes.ProtoId<StackPrototype>, int> { ["SteelOre"] = 10 },
                    userId,
                    1,
                    processor,
                    out _),
                Is.True);
        });

        await server.WaitAssertion(() =>
        {
            var looseUnits = 0;
            var containedUnits = 0;
            var shipmentQuery = server.EntMan.EntityQueryEnumerator<PrisonOreShipmentComponent, StackComponent, TransformComponent>();
            while (shipmentQuery.MoveNext(out var uid, out _, out var stack, out var xform))
            {
                if (xform.GridUid != shuttle)
                    continue;

                if (server.EntMan.HasComponent<InsideEntityStorageComponent>(uid))
                    containedUnits += stack.Count;
                else
                    looseUnits += stack.Count;
            }

            var crateCount = 0;
            var crateQuery = server.EntMan.EntityQueryEnumerator<MetaDataComponent, TransformComponent>();
            while (crateQuery.MoveNext(out var uid, out var metadata, out var xform))
            {
                if (xform.GridUid != shuttle || metadata.EntityPrototype?.ID != "CratePrisonOreShipment")
                    continue;

                crateCount++;
                Assert.That(server.EntMan.HasComponent<PrisonOreShipmentComponent>(uid), Is.False);
                Assert.That(server.EntMan.GetComponent<EntityStorageComponent>(uid).Contents.ContainedEntities, Is.Not.Empty);
            }

            Assert.Multiple(() =>
            {
                Assert.That(looseUnits, Is.EqualTo(5));
                Assert.That(containedUnits, Is.EqualTo(10));
                Assert.That(crateCount, Is.EqualTo(1));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task FindsDirectCargoShuttlePlacementWithoutPallets()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        EntityUid shuttle = default;
        EntityCoordinates coordinates = EntityCoordinates.Invalid;

        await server.WaitPost(() =>
        {
            var mapSystem = server.System<SharedMapSystem>();
            mapSystem.CreateMap(out var mapId);
            var grid = server.ResolveDependency<IMapManager>().CreateGridEntity(mapId);
            shuttle = grid.Owner;
            mapSystem.SetTile(grid.Owner, grid.Comp, Vector2i.Zero, new Tile(1));
            server.EntMan.EnsureComponent<CargoShuttleComponent>(shuttle);

            Assert.That(server.System<PrisonOreSystem>().TryGetCargoSpawnCoordinates(out coordinates), Is.True);
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(coordinates.EntityId, Is.EqualTo(shuttle));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ShipmentCreditFollowsStackSplitAndMerge()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        EntityUid source = default;
        EntityUid split = default;
        EntityUid recipient = default;
        var userId = new NetUserId(Guid.NewGuid());

        await server.WaitPost(() =>
        {
            source = server.EntMan.SpawnEntity("SteelOre", MapCoordinates.Nullspace);
            recipient = server.EntMan.SpawnEntity("SteelOre", MapCoordinates.Nullspace);
            var stackSystem = server.System<StackSystem>();
            var sourceStack = server.EntMan.GetComponent<StackComponent>(source);
            var recipientStack = server.EntMan.GetComponent<StackComponent>(recipient);
            stackSystem.SetCount((source, sourceStack), 30);
            stackSystem.SetCount((recipient, recipientStack), 10);

            server.System<PrisonOreSystem>().SetShipmentTracking(source, "SteelOre", 10, userId, 42, 1_000);

            split = stackSystem.Split(
                (source, sourceStack),
                12,
                server.EntMan.GetComponent<TransformComponent>(source).Coordinates)!.Value;

            var splitStack = server.EntMan.GetComponent<StackComponent>(split);
            Assert.That(stackSystem.TryMergeStacks((split, splitStack), (recipient, recipientStack), out _), Is.True);
        });

        await server.WaitAssertion(() =>
        {
            var shipment = server.EntMan.GetComponent<PrisonOreShipmentComponent>(recipient);
            Assert.Multiple(() =>
            {
                Assert.That(shipment.Ores["SteelOre"], Is.EqualTo(10));
                Assert.That(shipment.Contributions[0].ReductionTicks, Is.EqualTo(1_000));
                if (server.EntMan.TryGetComponent<PrisonOreShipmentComponent>(source, out var sourceShipment))
                    Assert.That(sourceShipment.Ores, Is.Empty);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task OreMinedOnPrisonMapBecomesEligible()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        EntityUid[] minedOres = [];

        await server.WaitPost(() =>
        {
            var mapSystem = server.System<SharedMapSystem>();
            mapSystem.CreateMap(out var mapId);
            server.EntMan.SpawnEntity("SpawnPointPrisoner", new MapCoordinates(Vector2.Zero, mapId));
            var vein = server.EntMan.SpawnEntity("MeteorRockCoal", new MapCoordinates(Vector2.One, mapId));

            server.EntMan.EventBus.RaiseLocalEvent(vein, new DestructionEventArgs());

            var query = server.EntMan.EntityQueryEnumerator<PrisonMinedOreComponent, StackComponent, TransformComponent>();
            var result = new List<EntityUid>();
            while (query.MoveNext(out var uid, out var mined, out var stack, out var xform))
            {
                if (xform.MapID != mapId)
                    continue;

                Assert.That(mined.EligibleUnits, Is.EqualTo(stack.Count));
                result.Add(uid);
            }

            minedOres = result.ToArray();
        });

        Assert.That(minedOres, Is.Not.Empty, "Ore spawned from a prison-map vein must be marked as eligible.");
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SplitMovesOnlyEligibleUnits()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        EntityUid source = default;
        EntityUid split = default;

        await server.WaitPost(() =>
        {
            source = server.EntMan.SpawnEntity("SteelOre", MapCoordinates.Nullspace);
            var stack = server.EntMan.GetComponent<StackComponent>(source);
            server.System<StackSystem>().SetCount((source, stack), 30);
            server.System<PrisonOreSystem>().SetEligibleUnits(source, 10);

            split = server.System<StackSystem>().Split(
                (source, stack),
                12,
                server.EntMan.GetComponent<TransformComponent>(source).Coordinates)!.Value;
        });

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.GetComponent<StackComponent>(source).Count, Is.EqualTo(18));
                Assert.That(server.EntMan.GetComponent<StackComponent>(split).Count, Is.EqualTo(12));
                Assert.That(
                    server.EntMan.GetComponent<PrisonMinedOreComponent>(source).EligibleUnits,
                    Is.EqualTo(0));
                Assert.That(server.EntMan.GetComponent<PrisonMinedOreComponent>(split).EligibleUnits, Is.EqualTo(10));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MergeConservesEligibleUnitsInMixedStacks()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        EntityUid donor = default;
        EntityUid recipient = default;

        await server.WaitPost(() =>
        {
            donor = server.EntMan.SpawnEntity("SteelOre", MapCoordinates.Nullspace);
            recipient = server.EntMan.SpawnEntity("SteelOre", MapCoordinates.Nullspace);
            var donorStack = server.EntMan.GetComponent<StackComponent>(donor);
            var recipientStack = server.EntMan.GetComponent<StackComponent>(recipient);
            var stackSystem = server.System<StackSystem>();
            stackSystem.SetCount((donor, donorStack), 20);
            stackSystem.SetCount((recipient, recipientStack), 10);
            server.System<PrisonOreSystem>().SetEligibleUnits(donor, 7);

            Assert.That(
                stackSystem.TryMergeStacks((donor, donorStack), (recipient, recipientStack), out var transferred),
                Is.True);
            Assert.That(transferred, Is.EqualTo(20));
        });

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.GetComponent<StackComponent>(recipient).Count, Is.EqualTo(30));
                Assert.That(server.EntMan.GetComponent<PrisonMinedOreComponent>(recipient).EligibleUnits, Is.EqualTo(7));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RewardOnlyChangesLatestTemporaryPrisonBan()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var database = server.ResolveDependency<IServerDbManager>();
        var prison = server.System<PrisonSystem>();
        var userId = new NetUserId(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var oldExpiration = now + TimeSpan.FromHours(2);
        var latestExpiration = now + TimeSpan.FromHours(3);

        var oldBan = await AddPrisonBan(database, userId, now - TimeSpan.FromMinutes(2), oldExpiration);
        var latestBan = await AddPrisonBan(database, userId, now - TimeSpan.FromMinutes(1), latestExpiration);

        Assert.That(
            await prison.TryReduceSentence(userId, oldBan.Id!.Value, TimeSpan.FromMinutes(5)),
            Is.EqualTo(TimeSpan.Zero),
            "Ore from an older sentence must not reduce a superseded ban.");
        Assert.That((await database.GetBanAsync(oldBan.Id.Value))!.ExpirationTime, Is.EqualTo(oldExpiration));

        Assert.That(
            await prison.TryReduceSentence(userId, latestBan.Id!.Value, TimeSpan.FromMinutes(5)),
            Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(
            (await database.GetBanAsync(latestBan.Id.Value))!.ExpirationTime,
            Is.EqualTo(latestExpiration - TimeSpan.FromMinutes(5)));

        await database.SetBanPrisonAccess(latestBan.Id.Value, false);
        Assert.That(
            await prison.TryReduceSentence(userId, latestBan.Id.Value, TimeSpan.FromMinutes(5)),
            Is.EqualTo(TimeSpan.Zero),
            "Revoking prison access must invalidate an ore shipment reward.");

        var permanentBan = await AddPrisonBan(database, userId, now, null);
        Assert.That(
            await prison.TryReduceSentence(userId, permanentBan.Id!.Value, TimeSpan.FromMinutes(5)),
            Is.EqualTo(TimeSpan.Zero),
            "A permanent sentence must never be reduced.");

        await pair.CleanReturnAsync();
    }

    private static Task<BanDef> AddPrisonBan(
        IServerDbManager database,
        NetUserId userId,
        DateTimeOffset banTime,
        DateTimeOffset? expiration)
    {
        return database.AddBanAsync(new BanDef(
            null,
            BanType.Server,
            ImmutableArray.Create(userId),
            ImmutableArray<(IPAddress address, int cidrMask)>.Empty,
            ImmutableArray<ImmutableTypedHwid>.Empty,
            banTime,
            expiration,
            ImmutableArray<int>.Empty,
            TimeSpan.Zero,
            "prison ore test",
            NoteSeverity.Minor,
            null,
            null,
            sendToPrison: true));
    }
}
