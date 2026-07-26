// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

#nullable enable

using System.Linq;
using Content.Server.DeadSpace.RoundEnd;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Languages.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.DeadSpace.RoundEnd;

[TestFixture]
[NonParallelizable]
public sealed class RoundEndManifestStatsTests
{
    private static readonly ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";
    private const string TestMobProto = "RoundEndManifestStatsTestMob";
    private const string MindlessMobProto = "RoundEndManifestStatsTestMindlessMob";
    private const string ProjectileProto = "RoundEndManifestStatsTestProjectile";
    private const string AntagRoleProto = "MindRoleTraitor";
    private const string LanguageProto = "GeneralLanguage";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: RoundEndManifestStatsTestMob
  components:
  - type: MindContainer
  - type: Damageable
    damageContainer: Biological
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Dead
- type: entity
  id: RoundEndManifestStatsTestMindlessMob
  components:
  - type: Damageable
    damageContainer: Biological
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Dead
- type: entity
  id: RoundEndManifestStatsTestProjectile
  components:
  - type: Projectile
    damage:
      types:
        Blunt: 0
";

    [Test]
    public async Task KillAndAssistAreAssignedToAntagMinds()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitPost(() =>
        {
            var context = GetContext(server);
            var antagA = SpawnPlayerMind(context, antag: true);
            var antagB = SpawnPlayerMind(context, antag: true);
            var target = SpawnPlayerMind(context, antag: false);

            Damage(context, target.Entity, antagA.Entity, 25);
            Damage(context, target.Entity, antagB.Entity, 120);

            AssertStats(context.ManifestStats, antagA.Mind, kills: 0, assists: 1);
            AssertStats(context.ManifestStats, antagB.Mind, kills: 1, assists: 0);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EnvironmentalDeathCreditsLargestAntagContributor()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitPost(() =>
        {
            var context = GetContext(server);
            var antagA = SpawnPlayerMind(context, antag: true);
            var antagB = SpawnPlayerMind(context, antag: true);
            var target = SpawnPlayerMind(context, antag: false);

            Damage(context, target.Entity, antagA.Entity, 75);
            Damage(context, target.Entity, antagB.Entity, 20);
            Damage(context, target.Entity, null, 100);

            AssertStats(context.ManifestStats, antagA.Mind, kills: 1, assists: 0);
            AssertStats(context.ManifestStats, antagB.Mind, kills: 0, assists: 1);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task HealingRemovesOldAssistContribution()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitPost(() =>
        {
            var context = GetContext(server);
            var antagA = SpawnPlayerMind(context, antag: true);
            var antagB = SpawnPlayerMind(context, antag: true);
            var target = SpawnPlayerMind(context, antag: false);

            Damage(context, target.Entity, antagA.Entity, 80);
            Damage(context, target.Entity, null, -80);
            Damage(context, target.Entity, antagB.Entity, 120);

            AssertStats(context.ManifestStats, antagA.Mind, kills: 0, assists: 0);
            AssertStats(context.ManifestStats, antagB.Mind, kills: 1, assists: 0);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NonAntagAndSelfKillsDoNotCreateStats()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitPost(() =>
        {
            var context = GetContext(server);
            var nonAntag = SpawnPlayerMind(context, antag: false);
            var nonAntagTarget = SpawnPlayerMind(context, antag: false);
            var selfKiller = SpawnPlayerMind(context, antag: true);

            Damage(context, nonAntagTarget.Entity, nonAntag.Entity, 120);
            Damage(context, selfKiller.Entity, selfKiller.Entity, 120);

            AssertStats(context.ManifestStats, nonAntag.Mind, kills: 0, assists: 0);
            AssertStats(context.ManifestStats, selfKiller.Mind, kills: 0, assists: 0);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MindlessMobDeathsDoNotCreateStats()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitPost(() =>
        {
            var context = GetContext(server);
            var antagA = SpawnPlayerMind(context, antag: true);
            var antagB = SpawnPlayerMind(context, antag: true);
            var target = context.EntMan.SpawnEntity(MindlessMobProto, new MapCoordinates());

            Damage(context, target, antagA.Entity, 25);
            Damage(context, target, antagB.Entity, 120);

            AssertStats(context.ManifestStats, antagA.Mind, kills: 0, assists: 0);
            AssertStats(context.ManifestStats, antagB.Mind, kills: 0, assists: 0);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ProjectileExplosionOriginCreditsShooter()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitPost(() =>
        {
            var context = GetContext(server);
            var antag = SpawnPlayerMind(context, antag: true);
            var target = SpawnPlayerMind(context, antag: false);
            var projectile = context.EntMan.SpawnEntity(ProjectileProto, new MapCoordinates());
            context.EntMan.GetComponent<ProjectileComponent>(projectile).Shooter = antag.Entity;

            Damage(context, target.Entity, projectile, 120);

            AssertStats(context.ManifestStats, antag.Mind, kills: 1, assists: 0);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LastCharacterQuoteIsStoredByMindAfterTransferAndFallbackExists()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitPost(() =>
        {
            var context = GetContext(server);
            var antag = SpawnPlayerMind(context, antag: true);
            var newBody = context.EntMan.SpawnEntity(TestMobProto, new MapCoordinates());
            var fallbackMind = SpawnPlayerMind(context, antag: true);
            var firstQuote = "The void remembers my name.";
            var expectedQuote = "The last signal is mine.";
            var ghostQuote = "Ghost words should not replace this.";

            context.MindSystem.TransferTo(antag.Mind, newBody);
            RaiseSpoke(context, newBody, firstQuote);
            RaiseSpoke(context, newBody, expectedQuote);
            context.EntMan.EnsureComponent<GhostComponent>(newBody);
            RaiseSpoke(context, newBody, ghostQuote);

            Assert.That(context.ManifestStats.GetManifestStats(antag.Mind).Quote, Is.EqualTo(expectedQuote));
            Assert.That(
                context.ManifestStats.GetManifestStats(fallbackMind.Mind).Quote,
                Is.EqualTo(Loc.GetString("round-end-summary-window-antag-manifest-quote-fallback")));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DeathSnapshotsAreDeferredAndReplaced()
    {
        var settings = new PoolSettings
        {
            Connected = true,
            Dirty = true,
            DummyTicker = false
        };
        await using var pair = await PoolManager.GetServerClient(settings);
        var server = pair.Server;
        var context = GetContext(server);
        var body = pair.Player!.AttachedEntity!.Value;
        var mindId = pair.PlayerData!.Mind!.Value;
        var mind = context.EntMan.GetComponent<MindComponent>(mindId);
        context.ManifestStats.EnsureManifestEntry(mindId, mind);

        await server.WaitAssertion(() =>
        {
            context.MobStateSystem.ChangeMobState(body, MobState.Dead);
            Assert.That(context.ManifestStats.GetDisplaySnapshot(mindId), Is.Null);
        });
        await pair.RunTicksSync(1);

        EntityUid? firstSnapshot = null;
        await server.WaitAssertion(() =>
        {
            firstSnapshot = context.ManifestStats.GetDisplaySnapshot(mindId);
            Assert.That(firstSnapshot, Is.Not.Null);
            Assert.That(context.EntMan.EntityExists(firstSnapshot), Is.True);
        });

        await server.WaitAssertion(() =>
        {
            context.MobStateSystem.ChangeMobState(body, MobState.Alive);
            context.MobStateSystem.ChangeMobState(body, MobState.Dead);
            Assert.That(context.ManifestStats.GetDisplaySnapshot(mindId), Is.EqualTo(firstSnapshot));
        });
        await pair.RunTicksSync(1);

        EntityUid? secondSnapshot = null;
        await server.WaitAssertion(() =>
        {
            secondSnapshot = context.ManifestStats.GetDisplaySnapshot(mindId);
            Assert.Multiple(() =>
            {
                Assert.That(secondSnapshot, Is.Not.Null);
                Assert.That(secondSnapshot, Is.Not.EqualTo(firstSnapshot));
                Assert.That(context.EntMan.EntityExists(firstSnapshot), Is.False);
                Assert.That(context.EntMan.EntityExists(secondSnapshot), Is.True);
            });
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DeathDuringCapturedBulkDeleteDoesNotCreateManifestSnapshot()
    {
        var settings = new PoolSettings
        {
            Connected = true,
            Destructive = true,
            Dirty = true,
            DummyTicker = false
        };
        await using var pair = await PoolManager.GetServerClient(settings);
        var server = pair.Server;
        var context = GetContext(server);
        var body = pair.Player!.AttachedEntity!.Value;
        var mindId = pair.PlayerData!.Mind!.Value;
        var mind = context.EntMan.GetComponent<MindComponent>(mindId);
        context.ManifestStats.EnsureManifestEntry(mindId, mind);

        await server.WaitPost(() =>
        {
            var entities = context.EntMan.GetEntities().ToArray();
            context.MobStateSystem.ChangeMobState(body, MobState.Dead);

            foreach (var entity in entities)
                context.EntMan.DeleteEntity(entity);
        });
        await pair.RunTicksSync(2);

        Assert.That(
            context.EntMan.EntityCount,
            Is.Zero,
            string.Join(
                Environment.NewLine,
                context.EntMan.GetEntities().Select(uid => context.EntMan.ToPrettyString(uid).ToString())));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DeletingAllEntitiesDoesNotCreateManifestSnapshot()
    {
        var settings = new PoolSettings
        {
            Connected = true,
            Destructive = true,
            Dirty = true,
            DummyTicker = false
        };
        await using var pair = await PoolManager.GetServerClient(settings);
        var server = pair.Server;
        var context = GetContext(server);
        var mindId = pair.PlayerData!.Mind!.Value;
        var mind = context.EntMan.GetComponent<MindComponent>(mindId);
        context.ManifestStats.EnsureManifestEntry(mindId, mind);

        var consoleHost = server.ResolveDependency<IConsoleHost>();
        await server.WaitPost(() => consoleHost.ExecuteCommand("entities delete"));
        await pair.RunTicksSync(5);

        Assert.That(
            context.EntMan.EntityCount,
            Is.Zero,
            string.Join(
                Environment.NewLine,
                context.EntMan.GetEntities().Select(uid => context.EntMan.ToPrettyString(uid).ToString())));
        await pair.CleanReturnAsync();
    }

    private static TestContextData GetContext(RobustIntegrationTest.ServerIntegrationInstance server)
    {
        var entMan = server.ResolveDependency<IServerEntityManager>();
        return new TestContextData(
            entMan,
            server.ResolveDependency<IPrototypeManager>(),
            entMan.EntitySysManager.GetEntitySystem<SharedMindSystem>(),
            entMan.EntitySysManager.GetEntitySystem<SharedRoleSystem>(),
            entMan.EntitySysManager.GetEntitySystem<DamageableSystem>(),
            entMan.EntitySysManager.GetEntitySystem<MobStateSystem>(),
            entMan.EntitySysManager.GetEntitySystem<RoundEndManifestStatsSystem>());
    }

    private static TestMind SpawnPlayerMind(TestContextData context, bool antag)
    {
        var entity = context.EntMan.SpawnEntity(TestMobProto, new MapCoordinates());
        var mindEnt = context.MindSystem.CreateMind(null);
#pragma warning disable RA0002
        mindEnt.Comp.OriginalOwnerUserId = new NetUserId(Guid.NewGuid());
#pragma warning restore RA0002
        var mind = mindEnt.Owner;
        context.MindSystem.TransferTo(mind, entity);

        if (antag)
            context.RoleSystem.MindAddRole(mind, AntagRoleProto);

        return new TestMind(entity, mind);
    }

    private static void Damage(TestContextData context, EntityUid target, EntityUid? origin, int amount)
    {
        var damageable = context.EntMan.GetComponent<DamageableComponent>(target);
        var damageType = context.ProtoMan.Index(BluntDamageType);
        context.DamageableSystem.TryChangeDamage(
            (target, damageable),
            new DamageSpecifier(damageType, FixedPoint2.New(amount)),
            origin: origin);
    }

    private static void RaiseSpoke(TestContextData context, EntityUid source, string message)
    {
        var ev = new EntitySpokeEvent(
            source,
            message,
            message,
            message,
            new ProtoId<LanguagePrototype>(LanguageProto),
            null,
            null);

        context.EntMan.EventBus.RaiseLocalEvent(source, ev, true);
    }

    private static void AssertStats(RoundEndManifestStatsSystem system, EntityUid mind, int kills, int assists)
    {
        var stats = system.GetManifestStats(mind);
        Assert.Multiple(() =>
        {
            Assert.That(stats.Kills, Is.EqualTo(kills));
            Assert.That(stats.Assists, Is.EqualTo(assists));
        });
    }

    private readonly record struct TestMind(EntityUid Entity, EntityUid Mind);

    private readonly record struct TestContextData(
        IServerEntityManager EntMan,
        IPrototypeManager ProtoMan,
        SharedMindSystem MindSystem,
        SharedRoleSystem RoleSystem,
        DamageableSystem DamageableSystem,
        MobStateSystem MobStateSystem,
        RoundEndManifestStatsSystem ManifestStats);
}
