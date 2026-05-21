using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Robust.Shared.GameObjects;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible;

/// <summary>
/// Tests ensuring the correct operation of <see cref="SharedDestructibleSystem"/>.
/// </summary>
[TestFixture]
public sealed class DestructibleOverkillTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly IEntitySystemManager _sEntSysManager = default!;

    /// <summary>
    /// Test that an entity with consequences is destroyed cleanly when overkilled.
    /// </summary>
    [Test]
    public async Task EnsureOverkill()
    {
        var testMap = await Pair.CreateTestMap();

        // Entity count prior to spawning and destroying
        var baseEntityCount = SEntMan.EntityCount;

        EntityUid sDestructibleEntity = default;
        TestDestructibleListenerSystem sTestThresholdListenerSystem = null;

        // Spawn our test entity and threshold listener
        await Server.WaitPost(() =>
        {
            sDestructibleEntity = SEntMan.SpawnEntity(DestructibleDestructionEntityId, testMap.GridCoords);
            sTestThresholdListenerSystem = _sEntSysManager.GetEntitySystem<TestDestructibleListenerSystem>();
            sTestThresholdListenerSystem.ThresholdsReached.Clear();
        });

        await Server.WaitAssertion(() =>
        {
            var bruteDamageGroup = SProtoMan.Index<DamageGroupPrototype>(TestBruteDamageGroupId);
            DamageSpecifier bruteDamage = new(bruteDamageGroup, 200);

            // Hit the destructible with enough damage to overkill
            Assert.DoesNotThrow(() =>
            {
                SEntMan.System<DamageableSystem>().TryChangeDamage(sDestructibleEntity, bruteDamage, true);
            });

            // Our first threshold should be the overkill destruction
            var threshold = sTestThresholdListenerSystem.ThresholdsReached[0].Threshold;

            // Ensure that the threshold triggered and only has one behavior
            Assert.Multiple(() =>
            {
                Assert.That(threshold.Triggered, Is.True);
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(1));
            });

            var doActsBehavior = (DoActsBehavior)threshold.Behaviors.Single(b => b is DoActsBehavior);

            // Ensure that the one act in this behavior is destruction
            Assert.Multiple(() =>
            {
                Assert.That(doActsBehavior.HasAct(ThresholdActs.Destruction));
            });
        });

        await Server.WaitRunTicks(1);   // Wait for predicted delete
        Assert.That(SEntMan.EntityCount, Is.EqualTo(baseEntityCount), $"Overkill destructible test did not destroy cleanly.");
    }
}
