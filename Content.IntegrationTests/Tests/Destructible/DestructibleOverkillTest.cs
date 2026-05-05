using System.Linq;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible;

public sealed class DestructibleOverkillTest
{
    [Test]
    public async Task Test()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        var sEntityManager = server.ResolveDependency<IEntityManager>();
        var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
        var sEntitySystemManager = server.ResolveDependency<IEntitySystemManager>();

        var baseEntityCount = sEntityManager.EntityCount;;

        EntityUid sDestructibleEntity = default;
        TestDestructibleListenerSystem sTestThresholdListenerSystem = null;

        await server.WaitPost(() =>
        {
            var coordinates = testMap.GridCoords;

            sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleDestructionEntityId, coordinates);
            sTestThresholdListenerSystem = sEntitySystemManager.GetEntitySystem<TestDestructibleListenerSystem>();
            sTestThresholdListenerSystem.ThresholdsReached.Clear();
        });

        await server.WaitAssertion(() =>
        {
            var bruteDamageGroup = sPrototypeManager.Index<DamageGroupPrototype>(TestBruteDamageGroupId);
            DamageSpecifier bruteDamage = new(bruteDamageGroup, 200);

            Assert.DoesNotThrow(() =>
            {
                sEntityManager.System<DamageableSystem>().TryChangeDamage(sDestructibleEntity, bruteDamage, true);
            });

            var threshold = sTestThresholdListenerSystem.ThresholdsReached[0].Threshold;

            Assert.Multiple(() =>
            {
                Assert.That(threshold.Triggered, Is.True);
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(1));
            });

            var doActsBehavior = (DoActsBehavior)threshold.Behaviors.Single(b => b is DoActsBehavior);

            Assert.Multiple(() =>
            {
                Assert.That(doActsBehavior.HasAct(ThresholdActs.Destruction));
            });
        });

        await server.WaitRunTicks(1);   // Wait for predicted delete
        Assert.That(sEntityManager.EntityCount, Is.EqualTo(baseEntityCount), $"Overkill destructible test did not destroy cleanly.");

        await pair.CleanReturnAsync();
    }
}
