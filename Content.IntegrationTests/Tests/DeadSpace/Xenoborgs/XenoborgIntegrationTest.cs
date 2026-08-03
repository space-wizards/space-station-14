// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

#nullable enable

using System.Collections.Generic;
using System.Numerics;
using Content.Server.DeadSpace.Lavaland.Components;
using Content.Server.DeadSpace.Xenoborgs.Components;
using Content.Server.Physics.Controllers;
using Content.Server.Tiles;
using Content.Shared.Chasm;
using Content.Shared.Damage;
using Content.Shared.DeadSpace.Xenoborgs;
using Content.Shared.DeadSpace.Xenoborgs.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.DeadSpace.Xenoborgs;

[TestFixture]
public sealed class XenoborgIntegrationTest
{
    private static readonly Vector2 CorePosition = new(0.5f, 0.5f);
    private static readonly Vector2 EyeStartPosition = new(1.5f, 0.5f);

    [Test]
    public async Task MinerModulesAndTeleportation()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mapSystem = server.System<SharedMapSystem>();
        var turf = server.System<TurfSystem>();
        var transform = server.System<SharedTransformSystem>();
        var useDelay = server.System<UseDelaySystem>();
        var (_, gridUid) = await CreateTestGrid(server);
        var (_, awayGridUid) = await CreateTestGrid(server);

        await server.WaitAssertion(() =>
        {
            AssertMinerModuleExclusivity(entMan, gridUid);
            AssertJaunterBranches(entMan, transform, useDelay, awayGridUid, gridUid);
            AssertPortalGunBranches(entMan, mapSystem, turf, useDelay, gridUid);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MothershipCoreCollisionAndEyeMovement()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mover = server.System<MoverController>();
        var transform = server.System<SharedTransformSystem>();
        var (_, gridUid) = await CreateTestGrid(server);

        (EntityUid Core, EntityUid Eye) eyeState = default;
        await server.WaitAssertion(() =>
        {
            eyeState = OpenMothershipEye(entMan, transform, gridUid);
        });

        EntityUid collisionXenoborg = default;
        await server.WaitPost(() =>
        {
            collisionXenoborg = entMan.SpawnEntity(
                "XenoborgEngi",
                new EntityCoordinates(gridUid, new Vector2(0.5f, -0.5f)));
            var input = entMan.GetComponent<InputMoverComponent>(collisionXenoborg);
            mover.SetVelocityDirection((collisionXenoborg, input), Direction.North, ushort.MaxValue, true);

            var eyeInput = entMan.GetComponent<InputMoverComponent>(eyeState.Eye);
            mover.SetVelocityDirection((eyeState.Eye, eyeInput), Direction.East, ushort.MaxValue, true);
        });

        await server.WaitRunTicks(30);
        await server.WaitAssertion(() =>
        {
            var input = entMan.GetComponent<InputMoverComponent>(collisionXenoborg);
            mover.SetVelocityDirection((collisionXenoborg, input), Direction.North, ushort.MaxValue, false);
            var eyeInput = entMan.GetComponent<InputMoverComponent>(eyeState.Eye);
            mover.SetVelocityDirection((eyeState.Eye, eyeInput), Direction.East, ushort.MaxValue, false);

            Assert.Multiple(() =>
            {
                Assert.That(
                    entMan.GetComponent<TransformComponent>(collisionXenoborg).LocalPosition.Y,
                    Is.LessThan(CorePosition.Y),
                    "A moving xenoborg passed through the mothership core collider.");
                Assert.That(
                    entMan.GetComponent<TransformComponent>(eyeState.Eye).LocalPosition,
                    Is.Not.EqualTo(EyeStartPosition),
                    "The projected mothership eye did not move after receiving movement input.");
                Assert.That(
                    entMan.GetComponent<TransformComponent>(eyeState.Core).LocalPosition,
                    Is.EqualTo(CorePosition));
            });

            CloseMothershipEye(entMan, eyeState.Core, eyeState.Eye);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MothershipEyeInteractionsStayOnMothershipGrid()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mapManager = server.ResolveDependency<IMapManager>();
        var map = server.System<SharedMapSystem>();
        var interaction = server.System<SharedInteractionSystem>();
        var transform = server.System<SharedTransformSystem>();
        var (mapId, gridUid) = await CreateTestGrid(server);

        (EntityUid Core, EntityUid Eye) eyeState = default;
        await server.WaitAssertion(() =>
        {
            eyeState = OpenMothershipEye(entMan, transform, gridUid);
        });

        await server.WaitAssertion(() =>
        {
            var airlock = entMan.SpawnEntity(
                "AirlockXenoborgLocked",
                new EntityCoordinates(gridUid, new Vector2(2.5f, 0.5f)));
            var button = entMan.SpawnEntity(
                "LockableButtonLawyer",
                new EntityCoordinates(gridUid, new Vector2(3.5f, 0.5f)));

            transform.SetCoordinates(eyeState.Eye, entMan.GetComponent<TransformComponent>(airlock).Coordinates);
            Assert.That(interaction.InteractionActivate(eyeState.Core, airlock), Is.True);

            transform.SetCoordinates(eyeState.Eye, entMan.GetComponent<TransformComponent>(button).Coordinates);
            Assert.That(interaction.InteractionActivate(eyeState.Core, button), Is.True);

            var foreignGrid = mapManager.CreateGridEntity(mapId);
            map.SetTile(foreignGrid.Owner, foreignGrid.Comp, Vector2i.Zero, new Tile(1));
            transform.SetWorldPosition(foreignGrid.Owner, new Vector2(100f, 100f));
            var foreignButton = entMan.SpawnEntity(
                "SignalButton",
                new EntityCoordinates(foreignGrid.Owner, new Vector2(0.5f, 0.5f)));
            Assert.Multiple(() =>
            {
                Assert.That(interaction.InRangeUnobstructed(eyeState.Core, foreignButton), Is.False);
                Assert.That(interaction.InteractionActivate(eyeState.Core, foreignButton), Is.False);
            });

            CloseMothershipEye(entMan, eyeState.Core, eyeState.Eye);
        });

        await pair.CleanReturnAsync();
    }

    private static async Task<(MapId MapId, EntityUid GridUid)> CreateTestGrid(
        RobustIntegrationTest.ServerIntegrationInstance server)
    {
        var mapManager = server.ResolveDependency<IMapManager>();
        var mapSystem = server.System<SharedMapSystem>();
        EntityUid gridUid = default;
        MapId mapId = default;

        await server.WaitPost(() =>
        {
            mapSystem.CreateMap(out mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            gridUid = grid.Owner;

            for (var x = -4; x <= 4; x++)
            {
                for (var y = -4; y <= 4; y++)
                    mapSystem.SetTile(grid.Owner, grid.Comp, new Vector2i(x, y), new Tile(1));
            }
        });

        return (mapId, gridUid);
    }

    private static void AssertMinerModuleExclusivity(IEntityManager entMan, EntityUid gridUid)
    {
        var coordinates = new EntityCoordinates(gridUid, CorePosition);
        var installedMinerModule = entMan.SpawnEntity("XenoborgModuleMiner", coordinates);
        var advancedMinerModule = entMan.SpawnEntity("XenoborgModuleAdvancedMiner", coordinates);
        var ordinaryModule = entMan.SpawnEntity("XenoborgModuleHeavyLaser", coordinates);

        var minerAttempt = new BorgModuleRelayedEvent<BorgModuleInsertAttemptEvent>(
            new BorgModuleInsertAttemptEvent(advancedMinerModule));
        entMan.EventBus.RaiseLocalEvent(installedMinerModule, ref minerAttempt);
        Assert.That(minerAttempt.Args.Cancelled, Is.True);

        var ordinaryAttempt = new BorgModuleRelayedEvent<BorgModuleInsertAttemptEvent>(
            new BorgModuleInsertAttemptEvent(ordinaryModule));
        entMan.EventBus.RaiseLocalEvent(installedMinerModule, ref ordinaryAttempt);
        Assert.That(ordinaryAttempt.Args.Cancelled, Is.False);
    }

    private static void AssertPortalGunBranches(
        IEntityManager entMan,
        SharedMapSystem mapSystem,
        TurfSystem turf,
        UseDelaySystem useDelay,
        EntityUid gridUid)
    {
        var grid = entMan.GetComponent<MapGridComponent>(gridUid);

        var gunUser = entMan.SpawnEntity(
            "XenoborgMiner",
            new EntityCoordinates(gridUid, CorePosition));
        var cooldownGun = entMan.SpawnEntity(
            "WeaponXenoborgPortalGun",
            entMan.GetComponent<TransformComponent>(gunUser).Coordinates);
        var cooldownGunComponent = entMan.GetComponent<GunComponent>(cooldownGun);
        var firstAttempt = new ShotAttemptedEvent
        {
            User = gunUser,
            Used = (cooldownGun, cooldownGunComponent),
        };
        entMan.EventBus.RaiseLocalEvent(cooldownGun, ref firstAttempt);
        Assert.That(firstAttempt.Cancelled, Is.False);

        var shot = new GunShotEvent(gunUser, []);
        entMan.EventBus.RaiseLocalEvent(cooldownGun, ref shot);
        var repeatedAttempt = new ShotAttemptedEvent
        {
            User = gunUser,
            Used = (cooldownGun, cooldownGunComponent),
        };
        entMan.EventBus.RaiseLocalEvent(cooldownGun, ref repeatedAttempt);
        Assert.That(repeatedAttempt.Cancelled, Is.True);

        (TimeSpan AppliedCooldown, XenoborgPortalGunComponent PortalGun) FireAt(EntityUid target)
        {
            var targetCoordinates = entMan.GetComponent<TransformComponent>(target).Coordinates;
            var gun = entMan.SpawnEntity("WeaponXenoborgPortalGun", targetCoordinates);
            var portalGun = entMan.GetComponent<XenoborgPortalGunComponent>(gun);
            var projectile = entMan.SpawnEntity("XenoborgPortalBolt", targetCoordinates);
            entMan.EventBus.RaiseLocalEvent(gun, new AmmoShotEvent
            {
                FiredProjectiles = [projectile],
            });

            var hit = new ProjectileHitEvent(new DamageSpecifier(), target);
            entMan.EventBus.RaiseLocalEvent(projectile, ref hit);

            Assert.That(entMan.TryGetComponent<UseDelayComponent>(gun, out var delay), Is.True);
            Assert.That(useDelay.TryGetDelayInfo((gun, delay), out var info), Is.True);
            return (info!.Length, portalGun);
        }

        var goliath = entMan.SpawnEntity(
            "MobGoliath",
            new EntityCoordinates(gridUid, CorePosition));
        var goliathDestinations = GetSafeDestinations(entMan, mapSystem, turf, gridUid, grid, goliath);
        var goliathOrigin = entMan.GetComponent<TransformComponent>(goliath).LocalPosition;
        var (unstunnedCooldown, unstunnedGun) = FireAt(goliath);
        var goliathXform = entMan.GetComponent<TransformComponent>(goliath);
        Assert.Multiple(() =>
        {
            Assert.That(goliathXform.GridUid, Is.EqualTo(gridUid));
            Assert.That(goliathXform.LocalPosition, Is.Not.EqualTo(goliathOrigin));
        });
        AssertSafeTile(entMan, turf, goliath, goliathDestinations);

        var stunnedTarget = entMan.SpawnEntity(
            "MobHuman",
            new EntityCoordinates(gridUid, EyeStartPosition));
        entMan.EnsureComponent<StunnedComponent>(stunnedTarget);
        var stunnedDestinations = GetSafeDestinations(entMan, mapSystem, turf, gridUid, grid, stunnedTarget);
        var stunnedOrigin = entMan.GetComponent<TransformComponent>(stunnedTarget).LocalPosition;
        var (stunnedCooldown, stunnedGun) = FireAt(stunnedTarget);
        Assert.That(
            entMan.GetComponent<TransformComponent>(stunnedTarget).LocalPosition,
            Is.Not.EqualTo(stunnedOrigin));
        AssertSafeTile(entMan, turf, stunnedTarget, stunnedDestinations);

        var nonLivingTarget = entMan.SpawnEntity(
            "Crowbar",
            new EntityCoordinates(gridUid, new Vector2(2.5f, 0.5f)));
        var nonLivingOrigin = entMan.GetComponent<TransformComponent>(nonLivingTarget).Coordinates;
        var (missCooldown, missedGun) = FireAt(nonLivingTarget);
        Assert.That(
            entMan.GetComponent<TransformComponent>(nonLivingTarget).Coordinates,
            Is.EqualTo(nonLivingOrigin));

        var bossTarget = entMan.SpawnEntity(
            "MobGoliath",
            new EntityCoordinates(gridUid, new Vector2(3.5f, 0.5f)));
        entMan.EnsureComponent<LavalandBossComponent>(bossTarget);
        var bossOrigin = entMan.GetComponent<TransformComponent>(bossTarget).Coordinates;
        var (bossCooldown, bossGun) = FireAt(bossTarget);
        Assert.That(
            entMan.GetComponent<TransformComponent>(bossTarget).Coordinates,
            Is.EqualTo(bossOrigin));

        Assert.Multiple(() =>
        {
            Assert.That(unstunnedCooldown, Is.EqualTo(unstunnedGun.UnstunnedCooldown));
            Assert.That(stunnedCooldown, Is.EqualTo(stunnedGun.StunnedCooldown));
            Assert.That(missCooldown, Is.EqualTo(missedGun.MissCooldown));
            Assert.That(bossCooldown, Is.EqualTo(bossGun.MissCooldown));
        });
    }

    private static (EntityUid Core, EntityUid Eye) OpenMothershipEye(
        IEntityManager entMan,
        SharedTransformSystem transform,
        EntityUid gridUid)
    {
        var core = entMan.SpawnEntity("MothershipCore", new EntityCoordinates(gridUid, CorePosition));
        var open = new ToggleMothershipEyeEvent { Performer = core };
        entMan.EventBus.RaiseLocalEvent(core, open);
        Assert.That(open.Handled, Is.True);

        var eyes = new List<EntityUid>();
        var query = entMan.AllEntityQueryEnumerator<MothershipEyeComponent>();
        while (query.MoveNext(out var uid, out var eye))
        {
            if (eye.Core == core)
                eyes.Add(uid);
        }

        Assert.That(eyes, Has.Count.EqualTo(1));
        var eyeUid = eyes[0];
        Assert.Multiple(() =>
        {
            Assert.That(entMan.GetComponent<EyeComponent>(core).Target, Is.EqualTo(eyeUid));
            Assert.That(entMan.GetComponent<RelayInputMoverComponent>(core).RelayEntity, Is.EqualTo(eyeUid));
            Assert.That(entMan.GetComponent<InputMoverComponent>(core).CanMove, Is.False);
            Assert.That(entMan.GetComponent<InputMoverComponent>(eyeUid).CanMove, Is.True);
        });

        transform.SetCoordinates(eyeUid, new EntityCoordinates(gridUid, EyeStartPosition));
        Assert.That(
            entMan.GetComponent<TransformComponent>(eyeUid).LocalPosition,
            Is.EqualTo(EyeStartPosition));

        transform.SetCoordinates(eyeUid, new EntityCoordinates(gridUid, new Vector2(100.5f, 100.5f)));
        var restrictedXform = entMan.GetComponent<TransformComponent>(eyeUid);
        Assert.Multiple(() =>
        {
            Assert.That(restrictedXform.GridUid, Is.EqualTo(gridUid));
            Assert.That(restrictedXform.LocalPosition, Is.EqualTo(EyeStartPosition));
        });

        return (core, eyeUid);
    }

    private static void CloseMothershipEye(IEntityManager entMan, EntityUid core, EntityUid eyeUid)
    {
        var close = new ToggleMothershipEyeEvent { Performer = core };
        entMan.EventBus.RaiseLocalEvent(core, close);
        Assert.Multiple(() =>
        {
            Assert.That(close.Handled, Is.True);
            Assert.That(entMan.GetComponent<EyeComponent>(core).Target, Is.Null);
            Assert.That(entMan.HasComponent<RelayInputMoverComponent>(core), Is.False);
            Assert.That(entMan.GetComponent<InputMoverComponent>(core).CanMove, Is.False);
            Assert.That(entMan.IsQueuedForDeletion(eyeUid), Is.True);
        });
    }

    private static HashSet<Vector2i> GetSafeDestinations(
        IEntityManager entMan,
        SharedMapSystem mapSystem,
        TurfSystem turf,
        EntityUid gridUid,
        MapGridComponent grid,
        EntityUid target)
    {
        var xform = entMan.GetComponent<TransformComponent>(target);
        var physics = entMan.GetComponent<PhysicsComponent>(target);
        Assert.That(xform.MapUid, Is.Not.Null);

        var safeTiles = new HashSet<Vector2i>();
        foreach (var tile in mapSystem.GetAllTiles(gridUid, grid))
        {
            if (tile.Tile.IsEmpty ||
                turf.IsSpace(tile) ||
                turf.IsTileBlocked(tile, (CollisionGroup) physics.CollisionMask))
            {
                continue;
            }

            var anchored = mapSystem.GetAnchoredEntitiesEnumerator(gridUid, grid, tile.GridIndices);
            var hazard = false;
            while (anchored.MoveNext(out var anchoredUid))
            {
                if (anchoredUid != null &&
                    (entMan.HasComponent<ChasmComponent>(anchoredUid.Value) ||
                     entMan.HasComponent<TileEntityEffectComponent>(anchoredUid.Value)))
                {
                    hazard = true;
                    break;
                }
            }

            if (!hazard)
                safeTiles.Add(tile.GridIndices);
        }

        Assert.That(safeTiles, Is.Not.Empty);
        return safeTiles;
    }

    private static void AssertSafeTile(
        IEntityManager entMan,
        TurfSystem turf,
        EntityUid target,
        HashSet<Vector2i> safeDestinations)
    {
        var xform = entMan.GetComponent<TransformComponent>(target);
        Assert.That(xform.MapUid, Is.Not.Null);
        Assert.That(turf.TryGetTileRef(xform.Coordinates, out var tile), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(turf.IsSpace(tile!.Value), Is.False);
            Assert.That(safeDestinations, Does.Contain(tile.Value.GridIndices));
        });
    }

    private static void AssertJaunterBranches(
        IEntityManager entMan,
        SharedTransformSystem transform,
        UseDelaySystem useDelay,
        EntityUid awayGridUid,
        EntityUid mothershipGridUid)
    {
        var user = entMan.SpawnEntity(
            "XenoborgMiner",
            new EntityCoordinates(awayGridUid, CorePosition));
        var jaunter = entMan.SpawnEntity("XenoborgJaunter", entMan.GetComponent<TransformComponent>(user).Coordinates);
        var jaunterDelay = entMan.GetComponent<UseDelayComponent>(jaunter);
        Assert.That(useDelay.TryGetDelayInfo((jaunter, jaunterDelay), out var initialDelay), Is.True);
        var initialEndTime = initialDelay!.EndTime;
        var initialCoordinates = entMan.GetComponent<TransformComponent>(user).Coordinates;

        entMan.EventBus.RaiseLocalEvent(jaunter, new UseInHandEvent(user));

        Assert.That(entMan.GetComponent<TransformComponent>(user).Coordinates, Is.EqualTo(initialCoordinates));
        Assert.That(useDelay.TryGetDelayInfo((jaunter, jaunterDelay), out var failedDelay), Is.True);
        Assert.That(failedDelay!.EndTime, Is.EqualTo(initialEndTime));

        var core = entMan.SpawnEntity("MothershipCore", new EntityCoordinates(mothershipGridUid, CorePosition));
        entMan.EventBus.RaiseLocalEvent(jaunter, new UseInHandEvent(user));

        var userXform = entMan.GetComponent<TransformComponent>(user);
        Assert.Multiple(() =>
        {
            Assert.That(userXform.GridUid, Is.EqualTo(mothershipGridUid));
            Assert.That(transform.GetMapCoordinates(user).MapId, Is.EqualTo(transform.GetMapCoordinates(core).MapId));
            Assert.That(useDelay.IsDelayed((jaunter, jaunterDelay)), Is.True);
        });
    }
}
