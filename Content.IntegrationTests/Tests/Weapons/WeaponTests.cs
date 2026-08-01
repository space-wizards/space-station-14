using Content.IntegrationTests.Tests.Interaction;
using Content.Client.Animations;
using Content.Client.Projectiles;
using Content.Shared.Damage.Components;
using Content.Shared.DeadSpace.Player;
using Content.Shared.Projectiles;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Sound.Components;
using Content.Shared.Standing;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Content.IntegrationTests.Tests.Weapons;

public sealed class WeaponTests : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman"; // The default test mob only has one hand
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId SniperMosin = "WeaponSniperMosin";
    private const string HitscanAmmo = "PredictionTestHitscan";
    private const string PredictionTestGun = "PredictionTestGun";
    private const string PredictionTestProjectile = "PredictionTestProjectile";
    private const string PredictionRegressionProjectile = "PredictionRegressionProjectile";
    private const string PredictionRegressionTarget = "PredictionRegressionTarget";

    [TestPrototypes]
    private const string PredictionPrototypes = @"
- type: entity
  id: PredictionTestGun
  components:
  - type: Gun

- type: entity
  id: PredictionTestProjectile
  parent: BaseBullet
  components:
  - type: Ammo
  - type: TimedDespawn
    lifetime: 0.01
  - type: TriggerOnCollide
    fixtureID: projectile
  - type: TriggerOnTimedCollide
  - type: ActiveTriggerOnTimedCollide
  - type: TriggerOnProximity
    requiresAnchored: false
  - type: RandomTimerTrigger
    min: 1
    max: 1
  - type: DamageContacts
    damage:
      types:
        Heat: 1
  - type: DamageOnHighSpeedImpact
    damage:
      types:
        Blunt: 1
    soundHit:
      collection: MetalThud
  - type: EmitSoundOnCollide
    sound:
      collection: MetalThud

- type: entity
  id: PredictionTestHitscan
  components:
  - type: HitscanAmmo
  - type: HitscanBasicRaycast
  - type: HitscanBasicVisuals

- type: entity
  id: PredictionRegressionProjectile
  parent: BaseBullet
  components:
  - type: Ammo
    muzzleFlash: MuzzleFlashEffect

- type: entity
  id: PredictionRegressionTarget
  components:
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      target:
        shape:
          !type:PhysShapeAabb
          bounds: ""-0.25,-0.25,0.25,0.25""
        hard: true
        layer:
        - BulletImpassable
  - type: Damageable
";

    [Test]
    public async Task GunRequiresWieldTest()
    {
        var gunSystem = SEntMan.System<SharedGunSystem>();

        await AddAtmosphere(); // prevent the Urist from suffocating

        var urist = await SpawnTarget(MobHuman);
        var damageComp = Comp<DamageableComponent>(urist);

        var mosinNet = await PlaceInHands(SniperMosin);
        var mosinEnt = ToServer(mosinNet);

        await Pair.RunSeconds(2f); // Guns have a cooldown when picking them up.

        Assert.That(HasComp<GunRequiresWieldComponent>(mosinNet),
            "Looks like you've removed the 'GunRequiresWield' component from the mosin sniper." +
            "If this was intentional, please update WeaponTests.cs to reflect this change!");

        var startAmmo = gunSystem.GetAmmoCount(mosinEnt);
        var wieldComp = Comp<WieldableComponent>(mosinNet);

        Assert.That(startAmmo, Is.GreaterThan(0), "Mosin was spawned with no ammo!");
        Assert.That(wieldComp.Wielded, Is.False, "Mosin was spawned wielded!");

        await AttemptShoot(urist, false); // should fail due to not being wielded
        var updatedAmmo = gunSystem.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo,
            Is.EqualTo(startAmmo),
            "Mosin discharged ammo when the weapon should not have fired!");
        Assert.That(damageComp.TotalDamage.Value,
            Is.EqualTo(0),
            "Urist took damage when the weapon should not have fired!");

        await UseInHand();

        Assert.That(wieldComp.Wielded, Is.True, "Mosin failed to wield when interacted with!");

        await AttemptShoot(urist);
        updatedAmmo = gunSystem.GetAmmoCount(mosinEnt);

        Assert.That(updatedAmmo, Is.EqualTo(startAmmo - 1), "Mosin failed to discharge appropriate amount of ammo!");
        Assert.That(damageComp.TotalDamage.Value,
            Is.GreaterThan(0),
            "Mosin was fired but urist sustained no damage!");
    }

    [Test]
    public async Task HitscanVisualPredictionDoesNotDealDamageTest()
    {
        var targetNet = await SpawnTarget(MobHuman);
        var ammoNet = await Spawn(HitscanAmmo, PlayerCoords);

        await Server.WaitAssertion(() =>
        {
            var target = SEntMan.GetEntity(targetNet);
            var ammo = SEntMan.GetEntity(ammoNet);
            var player = SEntMan.GetEntity(Player);
            var damage = SEntMan.GetComponent<DamageableComponent>(target);
            var raycast = SEntMan.GetComponent<HitscanBasicRaycastComponent>(ammo);
            var hitscan = SEntMan.System<HitscanBasicRaycastSystem>();

            var before = damage.TotalDamage;
            var trace = hitscan.BuildVisualTrace(
                (ammo, raycast),
                SEntMan.GetCoordinates(PlayerCoords),
                Vector2.UnitX,
                player,
                target);

            Assert.That(trace, Is.Not.Null);
            Assert.That(trace!.Value.ImpactedEnt, Is.EqualTo(targetNet));
            Assert.That(damage.TotalDamage, Is.EqualTo(before));
        });
    }

    // DS14-start
    [Test]
    public async Task WeaponEffectPvsIncludesRemoteViewSubscribersTest()
    {
        var origin = new MapCoordinates(new Vector2(1000f, 1000f), MapId);
        EntityUid remoteView = default;
        EntityUid projectile = default;
        NetEntity projectileNet = default;
        var config = Server.ResolveDependency<IConfigurationManager>();
        var previousPvs = config.GetCVar(CVars.NetPVS);

        await Server.WaitPost(() => config.SetCVar(CVars.NetPVS, true));

        try
        {
            await Server.WaitPost(() =>
            {
                remoteView = SEntMan.SpawnEntity(null, origin);
                var viewSystem = SEntMan.System<SharedViewSubscriberSystem>();
                viewSystem.AddViewSubscriber(remoteView, ServerSession);

                var attachedOnly = Filter.Empty().AddPlayersByPvs(origin, entManager: SEntMan);
                Assert.That(attachedOnly.Recipients, Does.Not.Contain(ServerSession));

                attachedOnly.AddPlayersByViewSubscriptions(origin, entityManager: SEntMan);
                Assert.That(attachedOnly.Recipients, Does.Contain(ServerSession));

                projectile = SEntMan.SpawnEntity(PredictionRegressionProjectile, origin);
                projectileNet = SEntMan.GetNetEntity(projectile);
            });

            await RunTicks(10);

            await Client.WaitAssertion(() =>
            {
                Assert.That(CEntMan.TryGetEntity(projectileNet, out _), Is.True,
                    "Physical projectiles should already be replicated through subscribed PVS views.");
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                if (remoteView.IsValid() && SEntMan.EntityExists(remoteView))
                    SEntMan.System<SharedViewSubscriberSystem>().RemoveViewSubscriber(remoteView, ServerSession);

                if (projectile.IsValid() && SEntMan.EntityExists(projectile))
                    SEntMan.DeleteEntity(projectile);

                if (remoteView.IsValid() && SEntMan.EntityExists(remoteView))
                    SEntMan.DeleteEntity(remoteView);

                config.SetCVar(CVars.NetPVS, previousPvs);
            });
        }
    }
    // DS14-end

    [Test]
    public async Task ProjectileTargetingUsesCanonicalStandingStateTest()
    {
        var targetNet = await SpawnTarget(MobHuman);
        var ammoNet = await Spawn(HitscanAmmo, PlayerCoords);

        await Client.WaitAssertion(() =>
        {
            var target = CEntMan.GetEntity(targetNet);
            var ammo = CEntMan.GetEntity(ammoNet);
            var requireTarget = CEntMan.GetComponent<RequireProjectileTargetComponent>(target);
            var mobState = CEntMan.GetComponent<MobStateComponent>(target);
            var standing = CEntMan.GetComponent<StandingStateComponent>(target);
            var system = CEntMan.System<RequireProjectileTargetSystem>();
            var raycast = CEntMan.GetComponent<HitscanBasicRaycastComponent>(ammo);
            var hitscan = CEntMan.System<HitscanBasicRaycastSystem>();
            var coordinates = CEntMan.GetCoordinates(PlayerCoords);

#pragma warning disable RA0002
            requireTarget.Active = false;
            standing.Standing = false;
#pragma warning restore RA0002
            Assert.That(system.RequiresExplicitTarget((target, requireTarget)), Is.True);
            var downedTrace = hitscan.BuildVisualTrace(
                (ammo, raycast),
                coordinates,
                Vector2.UnitX,
                CPlayer,
                null);
            Assert.That(downedTrace, Is.Not.Null);
            Assert.That(downedTrace!.Value.ImpactedEnt, Is.Not.EqualTo(targetNet));

#pragma warning disable RA0002
            requireTarget.Active = true;
            standing.Standing = true;
#pragma warning restore RA0002
            Assert.That(system.RequiresExplicitTarget((target, requireTarget)), Is.False);
            Assert.That(system.RequiresExplicitTargetForPrediction((target, requireTarget)), Is.True);
            var standingTrace = hitscan.BuildVisualTrace(
                (ammo, raycast),
                coordinates,
                Vector2.UnitX,
                CPlayer,
                null);
            Assert.That(standingTrace, Is.Not.Null);
            Assert.That(standingTrace!.Value.ImpactedEnt, Is.EqualTo(targetNet));

#pragma warning disable RA0002
            requireTarget.Active = false;
            standing.Standing = true;
            mobState.CurrentState = MobState.PreCritical;
#pragma warning restore RA0002
            Assert.That(system.RequiresExplicitTarget((target, requireTarget)), Is.True);
            Assert.That(system.RequiresExplicitTargetForPrediction((target, requireTarget)), Is.True);
            var incapacitatedTrace = hitscan.BuildVisualTrace(
                (ammo, raycast),
                coordinates,
                Vector2.UnitX,
                CPlayer,
                null);
            Assert.That(incapacitatedTrace, Is.Not.Null);
            Assert.That(incapacitatedTrace!.Value.ImpactedEnt, Is.Not.EqualTo(targetNet));

#pragma warning disable RA0002
            mobState.CurrentState = MobState.Critical;
#pragma warning restore RA0002
            Assert.That(system.RequiresExplicitTargetForPrediction((target, requireTarget)), Is.True);

#pragma warning disable RA0002
            mobState.CurrentState = MobState.Dead;
#pragma warning restore RA0002
            Assert.That(system.RequiresExplicitTargetForPrediction((target, requireTarget)), Is.True);
        });
    }

    [Test]
    public async Task EnergyProjectileCollisionMasksMatchLegacyHitscanTest()
    {
        await Server.WaitAssertion(() =>
        {
            AssertProjectileMask("BulletLaser", CollisionGroup.Opaque);
            AssertProjectileMask("BasicHitscan", CollisionGroup.Opaque);
            AssertProjectileMask("DominatorDisabler", CollisionGroup.Opaque);
            AssertProjectileMask("BulletEnergyTurretLaser", CollisionGroup.Opaque);
            AssertProjectileMask("BulletEnergyTurretDisabler", CollisionGroup.Opaque);
            AssertProjectileMask(
                "BulletDisabler",
                CollisionGroup.Opaque | CollisionGroup.Impassable | CollisionGroup.BulletImpassable);
            AssertProjectileMask(
                "BulletTaser",
                CollisionGroup.Opaque | CollisionGroup.Impassable | CollisionGroup.BulletImpassable);
        });
    }

    [Test]
    public async Task DownedUntargetedProjectileDoesNotCollideTest()
    {
        var targetNet = await SpawnTarget(MobHuman);

        await Server.WaitAssertion(() =>
        {
            var target = SEntMan.GetEntity(targetNet);
            var projectile = SEntMan.SpawnEntity(PredictionTestProjectile, SEntMan.GetCoordinates(PlayerCoords));
            var standing = SEntMan.GetComponent<StandingStateComponent>(target);

            var targetBody = SEntMan.GetComponent<PhysicsComponent>(target);
            var projectileBody = SEntMan.GetComponent<PhysicsComponent>(projectile);
            var targetFixture = SEntMan.GetComponent<FixturesComponent>(target).Fixtures.Values.First();
            var projectileFixture = SEntMan.GetComponent<FixturesComponent>(projectile)
                .Fixtures[SharedProjectileSystem.ProjectileFixture];

#pragma warning disable RA0002
            standing.Standing = false;
#pragma warning restore RA0002
            AssertCollisionCancelled(true, "An untargeted projectile collided with a downed entity.");

            var targeted = SEntMan.AddComponent<TargetedProjectileComponent>(projectile);
#pragma warning disable RA0002
            targeted.Target = target;
#pragma warning restore RA0002
            AssertCollisionCancelled(false, "A projectile explicitly aimed at a downed entity passed through it.");

            SEntMan.DeleteEntity(projectile);

            void AssertCollisionCancelled(bool expected, string message)
            {
                var collision = new PreventCollideEvent(
                    target,
                    projectile,
                    targetBody,
                    projectileBody,
                    targetFixture,
                    projectileFixture);

                SEntMan.EventBus.RaiseLocalEvent(target, ref collision);
                Assert.That(collision.Cancelled, Is.EqualTo(expected), message);
            }
        });
    }

    private void AssertProjectileMask(EntProtoId prototype, CollisionGroup expected)
    {
        var uid = SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(PlayerCoords));
        var fixtures = SEntMan.GetComponent<FixturesComponent>(uid);

        Assert.That(fixtures.Fixtures[SharedProjectileSystem.ProjectileFixture].CollisionMask,
            Is.EqualTo((int) expected),
            $"{prototype} has a collision mask that does not match its former hitscan raycast.");

        SEntMan.DeleteEntity(uid);
    }

    [Test]
    public async Task PhysicalProjectilePredictionContractTest()
    {
        const uint predictionId = 42;
        var targetNet = await SpawnTarget(MobHuman);

        await Server.WaitAssertion(() =>
        {
            var target = SEntMan.GetEntity(targetNet);
            var coordinates = SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates;
            var gunUid = SEntMan.SpawnEntity(PredictionTestGun, coordinates);
            var projectile = SEntMan.SpawnEntity(PredictionTestProjectile, coordinates);
            var gun = SEntMan.GetComponent<GunComponent>(gunUid);
            var ammo = SEntMan.GetComponent<AmmoComponent>(projectile);
#pragma warning disable RA0002
            gun.PredictionId = predictionId;
            gun.Target = target;
#pragma warning restore RA0002

            SEntMan.System<Content.Server.Weapons.Ranged.Systems.GunSystem>().Shoot(
                (gunUid, gun),
                [(projectile, ammo)],
                coordinates,
                coordinates.Offset(Vector2.UnitX),
                out _,
                SPlayer);

            var marker = SEntMan.GetComponent<PredictedProjectileComponent>(projectile);
            Assert.That(marker.Shooter, Is.EqualTo(SPlayer));
            Assert.That(marker.PredictionId, Is.EqualTo(predictionId));
            Assert.That(marker.ProjectileIndex, Is.Zero);
            Assert.That(SEntMan.GetComponent<TargetedProjectileComponent>(projectile).Target, Is.EqualTo(target));

            SEntMan.DeleteEntity(projectile);
            SEntMan.DeleteEntity(gunUid);
        });

        await Client.WaitAssertion(() =>
        {
            var target = CEntMan.GetEntity(targetNet);
            var coordinates = CEntMan.GetComponent<TransformComponent>(CPlayer).Coordinates;
            var gunUid = CEntMan.SpawnEntity(PredictionTestGun, coordinates);
            var projectile = CEntMan.SpawnEntity(PredictionTestProjectile, coordinates);
            var gun = CEntMan.GetComponent<GunComponent>(gunUid);
            var ammo = CEntMan.GetComponent<AmmoComponent>(projectile);
#pragma warning disable RA0002
            gun.PredictionId = predictionId;
            gun.Target = target;
#pragma warning restore RA0002

            CEntMan.System<Content.Client.Weapons.Ranged.Systems.GunSystem>().Shoot(
                (gunUid, gun),
                [(projectile, ammo)],
                coordinates,
                coordinates.Offset(Vector2.UnitX),
                out _,
                CPlayer);

            var predicted = CEntMan.GetComponent<PredictedProjectileVisualComponent>(projectile);
            Assert.That(predicted.PredictionId, Is.EqualTo(predictionId));
            Assert.That(predicted.ProjectileIndex, Is.Zero);
            Assert.That(CEntMan.GetComponent<TargetedProjectileComponent>(projectile).Target, Is.EqualTo(target));
            Assert.That(CEntMan.HasComponent<TimedDespawnComponent>(projectile), Is.False);
            Assert.That(CEntMan.HasComponent<TriggerOnCollideComponent>(projectile), Is.False);
            Assert.That(CEntMan.HasComponent<TriggerOnTimedCollideComponent>(projectile), Is.False);
            Assert.That(CEntMan.HasComponent<ActiveTriggerOnTimedCollideComponent>(projectile), Is.False);
            Assert.That(CEntMan.HasComponent<TriggerOnProximityComponent>(projectile), Is.False);
            Assert.That(CEntMan.HasComponent<RandomTimerTriggerComponent>(projectile), Is.False);
            Assert.That(CEntMan.HasComponent<DamageContactsComponent>(projectile), Is.False);
            Assert.That(CEntMan.HasComponent<DamageOnHighSpeedImpactComponent>(projectile), Is.False);
            Assert.That(CEntMan.HasComponent<EmitSoundOnCollideComponent>(projectile), Is.False);
            Assert.That(
                CEntMan.GetComponent<FixturesComponent>(projectile).Fixtures,
                Does.Not.ContainKey(TriggerOnProximityComponent.FixtureID));

            CEntMan.DeleteEntity(projectile);
            CEntMan.DeleteEntity(gunUid);
        });
    }

    [Test]
    public async Task PredictionRegression_OpenStaticTargetIsDamaged()
    {
        var (target, _) = await RunPredictionRegressionHit();

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<DamageableComponent>(target).TotalDamage.Value, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task PredictionRegression_WallBlocksTargetAndTakesHit()
    {
        var (target, blocker) = await RunPredictionRegressionHit(new Vector2(0.5f, 0f));

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<DamageableComponent>(target).TotalDamage.Value, Is.Zero);
            Assert.That(blocker, Is.Not.Null);
            Assert.That(
                SEntMan.GetComponent<DamageableComponent>(blocker!.Value).TotalDamage.Value,
                Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task PredictionRegression_WallCornerBlocksTarget()
    {
        var (target, blocker) = await RunPredictionRegressionHit(new Vector2(0.5f, 0.45f));

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<DamageableComponent>(target).TotalDamage.Value, Is.Zero);
            Assert.That(blocker, Is.Not.Null);
            Assert.That(
                SEntMan.GetComponent<DamageableComponent>(blocker!.Value).TotalDamage.Value,
                Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task PredictionRegression_MuzzleFlashIsDeduplicatedAndTracksGun()
    {
        const uint predictionId = 77;
        var gunNet = await Spawn(PredictionTestGun, PlayerCoords);

        await Client.WaitAssertion(() =>
        {
            var gun = CEntMan.GetEntity(gunNet);
            var gunComponent = CEntMan.GetComponent<GunComponent>(gun);
            var coordinates = CEntMan.GetCoordinates(PlayerCoords);
            var projectileA = CEntMan.SpawnEntity(PredictionRegressionProjectile, coordinates);
            var projectileB = CEntMan.SpawnEntity(PredictionRegressionProjectile, coordinates);
            var gunSystem = CEntMan.System<Content.Client.Weapons.Ranged.Systems.GunSystem>();
            var before = GetMuzzleFlashes().ToHashSet();

#pragma warning disable RA0002
            gunComponent.PredictionId = predictionId;
#pragma warning restore RA0002

            gunSystem.Shoot(
                (gun, gunComponent),
                [(projectileA, CEntMan.GetComponent<AmmoComponent>(projectileA))],
                coordinates,
                coordinates.Offset(Vector2.UnitX),
                out _,
                CPlayer);
            gunSystem.Shoot(
                (gun, gunComponent),
                [(projectileB, CEntMan.GetComponent<AmmoComponent>(projectileB))],
                coordinates,
                coordinates.Offset(Vector2.UnitX),
                out _,
                CPlayer);

            var duplicate = new MuzzleFlashEvent(
                gunNet,
                "MuzzleFlashEffect",
                Angle.Zero,
                predictionId);
            CEntMan.EventBus.RaiseEvent(EventSource.Network, duplicate);
            CEntMan.EventBus.RaiseEvent(EventSource.Network, duplicate);

            var flashes = GetMuzzleFlashes().Where(uid => !before.Contains(uid)).ToList();
            Assert.That(flashes, Has.Count.EqualTo(1));
            Assert.That(CEntMan.GetComponent<TrackUserComponent>(flashes[0]).User, Is.EqualTo(gun));

            CEntMan.DeleteEntity(projectileA);
            CEntMan.DeleteEntity(projectileB);

            IEnumerable<EntityUid> GetMuzzleFlashes()
            {
                return CEntMan.GetEntities().Where(uid =>
                    CEntMan.TryGetComponent(uid, out MetaDataComponent metadata) &&
                    metadata.EntityPrototype?.ID == "MuzzleFlashEffect");
            }
        });
    }

    [Test]
    public async Task PredictionRegression_SelfTargetHitsProjectileAndHitscan()
    {
        var hitscanNet = await Spawn(HitscanAmmo, PlayerCoords);

        await Server.WaitAssertion(() =>
        {
            var projectile = SEntMan.SpawnEntity(
                PredictionRegressionProjectile,
                SEntMan.GetCoordinates(PlayerCoords));
            var projectileComponent = SEntMan.GetComponent<ProjectileComponent>(projectile);
            var projectileBody = SEntMan.GetComponent<PhysicsComponent>(projectile);
            var projectileFixture = SEntMan.GetComponent<FixturesComponent>(projectile)
                .Fixtures[SharedProjectileSystem.ProjectileFixture];
            var playerBody = SEntMan.GetComponent<PhysicsComponent>(SPlayer);
            var playerFixture = SEntMan.GetComponent<FixturesComponent>(SPlayer).Fixtures.Values.First(f => f.Hard);
            var targeted = SEntMan.AddComponent<TargetedProjectileComponent>(projectile);

#pragma warning disable RA0002
            projectileComponent.Shooter = SPlayer;
            targeted.Target = SPlayer;
#pragma warning restore RA0002
            AssertCollisionCancelled(false);

#pragma warning disable RA0002
            targeted.Target = default;
#pragma warning restore RA0002
            AssertCollisionCancelled(true);
            SEntMan.DeleteEntity(projectile);

            void AssertCollisionCancelled(bool expected)
            {
                var collision = new PreventCollideEvent(
                    projectile,
                    SPlayer,
                    projectileBody,
                    playerBody,
                    projectileFixture,
                    playerFixture);
                SEntMan.EventBus.RaiseLocalEvent(projectile, ref collision);
                Assert.That(collision.Cancelled, Is.EqualTo(expected));
            }
        });

        await Client.WaitAssertion(() =>
        {
            var hitscan = CEntMan.GetEntity(hitscanNet);
            var raycast = CEntMan.GetComponent<HitscanBasicRaycastComponent>(hitscan);
            var trace = CEntMan.System<HitscanBasicRaycastSystem>().BuildVisualTrace(
                (hitscan, raycast),
                CEntMan.GetCoordinates(PlayerCoords),
                Vector2.UnitY,
                CPlayer,
                CPlayer);

            Assert.That(trace, Is.Not.Null);
            Assert.That(trace!.Value.Distance, Is.Zero);
            Assert.That(trace.Value.ImpactedEnt, Is.EqualTo(Player));
        });
    }

    private async Task<(EntityUid Target, EntityUid? Blocker)> RunPredictionRegressionHit(
        Vector2? blockerOffset = null)
    {
        const uint predictionId = 101;
        var targetNetCoordinates = new NetCoordinates(
            PlayerCoords.NetEntity,
            PlayerCoords.Position + Vector2.UnitX);
        var targetNet = await Spawn(PredictionRegressionTarget, targetNetCoordinates);
        NetEntity? blockerNet = null;
        if (blockerOffset is { } offset)
        {
            blockerNet = await Spawn(
                PredictionRegressionTarget,
                new NetCoordinates(PlayerCoords.NetEntity, PlayerCoords.Position + offset));
        }

        MapCoordinates targetCoordinates = default;
        MapCoordinates projectileCoordinates = default;
        MapCoordinates contactCoordinates = default;
        await Server.WaitPost(() =>
        {
            var target = SEntMan.GetEntity(targetNet);
            var projectile = SEntMan.SpawnEntity(
                PredictionRegressionProjectile,
                SEntMan.GetCoordinates(PlayerCoords));
            var marker = SEntMan.AddComponent<PredictedProjectileComponent>(projectile);
            var projectileComponent = SEntMan.GetComponent<ProjectileComponent>(projectile);

#pragma warning disable RA0002
            marker.Shooter = SPlayer;
            marker.PredictionId = predictionId;
            marker.ProjectileIndex = 0;
            marker.Origin = Transform.GetMapCoordinates(projectile);
            projectileComponent.Shooter = SPlayer;
#pragma warning restore RA0002

            targetCoordinates = Transform.GetMapCoordinates(target);
            projectileCoordinates = new MapCoordinates(
                targetCoordinates.Position - new Vector2(0.35f, 0f),
                targetCoordinates.MapId);
            contactCoordinates = new MapCoordinates(
                targetCoordinates.Position - new Vector2(0.25f, 0f),
                targetCoordinates.MapId);
        });

        await RunTicks(1);
        await Client.WaitPost(() =>
        {
            CEntMan.RaisePredictiveEvent(new PredictedProjectileHitEvent(
                predictionId,
                0,
                new HashSet<(NetEntity, MapCoordinates, MapCoordinates, MapCoordinates)>
                {
                    (targetNet, targetCoordinates, projectileCoordinates, contactCoordinates),
                }));
        });
        await RunTicks(3);

        return (
            SEntMan.GetEntity(targetNet),
            blockerNet is { } blocker ? SEntMan.GetEntity(blocker) : null);
    }
}
