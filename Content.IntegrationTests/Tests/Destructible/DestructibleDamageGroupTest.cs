using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible
{
    [TestFixture]
    [TestOf(typeof(DamageGroupTrigger))]
    [TestOf(typeof(AndTrigger))]
    public sealed class DestructibleDamageGroupTest
    {
        [Test]
        public async Task AndTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var testMap = await pair.CreateTestMap();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var sEntitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            EntityUid sDestructibleEntity = default;
            DamageableComponent sDamageableComponent = null;
            TestDestructibleListenerSystem sTestThresholdListenerSystem = null;
            DamageableSystem sDamageableSystem = null;

            await server.WaitPost(() =>
            {
                var coordinates = testMap.GridCoords;

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleDamageGroupEntityId, coordinates);
                sDamageableComponent = sEntityManager.GetComponent<DamageableComponent>(sDestructibleEntity);

                sTestThresholdListenerSystem = sEntitySystemManager.GetEntitySystem<TestDestructibleListenerSystem>();
                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                sDamageableSystem = sEntitySystemManager.GetEntitySystem<DamageableSystem>();
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);
            });

            await server.WaitAssertion(() =>
            {
                var bruteDamageGroup = sPrototypeManager.Index<DamageGroupPrototype>(TestBruteDamageGroupId);
                var burnDamageGroup = sPrototypeManager.Index<DamageGroupPrototype>(TestBurnDamageGroupId);

                DamageSpecifier bruteDamage = new(bruteDamageGroup, FixedPoint2.New(5));
                DamageSpecifier burnDamage = new(burnDamageGroup, FixedPoint2.New(5));

                // Raise brute damage to 5
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage, true);

                // No thresholds reached yet, the earliest one is at 10 damage
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise brute damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage, true);

                // No threshold reached, burn needs to be 10 as well
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise burn damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, burnDamage * 2, true);

                // One threshold reached, brute 10 + burn 10
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));

                // Threshold brute 10 + burn 10
                var msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                var threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.Multiple(() =>
                {
                    Assert.That(threshold.Behaviors, Is.Empty);
                    Assert.That(threshold.Trigger, Is.Not.Null);
                    Assert.That(threshold.Triggered, Is.True);
                    Assert.That(threshold.Trigger, Is.InstanceOf<AndTrigger>());
                });

                var trigger = (AndTrigger)threshold.Trigger;

                Assert.Multiple(() =>
                {
                    Assert.That(trigger.Triggers[0], Is.InstanceOf<DamageGroupTrigger>());
                    Assert.That(trigger.Triggers[1], Is.InstanceOf<DamageGroupTrigger>());
                });

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Raise brute damage to 20
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * 2, true);

                // No new thresholds reached
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise burn damage to 20
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, burnDamage * 2, true);

                // No new thresholds reached
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Lower brute damage to 0
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * -10);
                Assert.Multiple(() =>
                {
                    Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(FixedPoint2.New(20)));

                    // No new thresholds reached, healing should not trigger it
                    Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);
                });

                // Raise brute damage back up to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * 2, true);

                // 10 brute + 10 burn threshold reached, brute was healed and brought back to its threshold amount and slash stayed the same
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal both classes of damage to 0
                sDamageableSystem.SetAllDamage(sDestructibleEntity, sDamageableComponent, 0);

                // No new thresholds reached, healing should not trigger it
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise brute damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * 2, true);

                // No new thresholds reached
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise burn damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, burnDamage * 2, true);

                // Both classes of damage were healed and then raised again, the threshold should have been reached as triggers once is default false
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));

                // Threshold brute 10 + burn 10
                msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.Multiple(() =>
                {
                    Assert.That(threshold.Behaviors, Is.Empty);
                    Assert.That(threshold.Trigger, Is.Not.Null);
                    Assert.That(threshold.Triggered, Is.True);
                    Assert.That(threshold.Trigger, Is.InstanceOf<AndTrigger>());
                });

                trigger = (AndTrigger)threshold.Trigger;

                Assert.Multiple(() =>
                {
                    Assert.That(trigger.Triggers[0], Is.InstanceOf<DamageGroupTrigger>());
                    Assert.That(trigger.Triggers[1], Is.InstanceOf<DamageGroupTrigger>());
                });

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Change triggers once to true
                threshold.TriggersOnce = true;

                // Heal brute and burn back to 0
                sDamageableSystem.SetAllDamage(sDestructibleEntity, sDamageableComponent, 0);

                // No new thresholds reached from healing
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise brute damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * 2, true);

                // No new thresholds reached
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise burn damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, burnDamage * 2, true);

                // No new thresholds reached as triggers once is set to true and it already triggered before
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);
            });
            await pair.CleanReturnAsync();
        }
    }
}
