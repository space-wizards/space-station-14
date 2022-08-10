using System.Threading.Tasks;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
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
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

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
                sDamageableComponent = IoCManager.Resolve<IEntityManager>().GetComponent<DamageableComponent>(sDestructibleEntity);

                sTestThresholdListenerSystem = sEntitySystemManager.GetEntitySystem<TestDestructibleListenerSystem>();
                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                sDamageableSystem = sEntitySystemManager.GetEntitySystem<DamageableSystem>();
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);
            });

            await server.WaitAssertion(() =>
            {
                var bruteDamageGroup = sPrototypeManager.Index<DamageGroupPrototype>("TestBrute");
                var burnDamageGroup = sPrototypeManager.Index<DamageGroupPrototype>("TestBurn");

                DamageSpecifier bruteDamage = new(bruteDamageGroup, FixedPoint2.New(5));
                DamageSpecifier burnDamage = new(burnDamageGroup, FixedPoint2.New(5));

                // Raise brute damage to 5
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage, true);

                // No thresholds reached yet, the earliest one is at 10 damage
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise brute damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage, true);

                // No threshold reached, burn needs to be 10 as well
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise burn damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, burnDamage * 2, true);

                // One threshold reached, brute 10 + burn 10
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold brute 10 + burn 10
                var msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                var threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);
                Assert.IsInstanceOf<AndTrigger>(threshold.Trigger);

                var trigger = (AndTrigger) threshold.Trigger;

                Assert.IsInstanceOf<DamageGroupTrigger>(trigger.Triggers[0]);
                Assert.IsInstanceOf<DamageGroupTrigger>(trigger.Triggers[1]);

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Raise brute damage to 20
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * 2, true);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise burn damage to 20
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, burnDamage * 2, true);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Lower brute damage to 0
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * -10);
                Assert.That(sDamageableComponent.TotalDamage,Is.EqualTo(FixedPoint2.New(20)));

                // No new thresholds reached, healing should not trigger it
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise brute damage back up to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * 2, true);

                // 10 brute + 10 burn threshold reached, brute was healed and brought back to its threshold amount and slash stayed the same
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal both classes of damage to 0
                sDamageableSystem.SetAllDamage(sDamageableComponent, 0);

                // No new thresholds reached, healing should not trigger it
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise brute damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * 2, true);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise burn damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, burnDamage * 2, true);

                // Both classes of damage were healed and then raised again, the threshold should have been reached as triggers once is default false
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold brute 10 + burn 10
                msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);
                Assert.IsInstanceOf<AndTrigger>(threshold.Trigger);

                trigger = (AndTrigger) threshold.Trigger;

                Assert.IsInstanceOf<DamageGroupTrigger>(trigger.Triggers[0]);
                Assert.IsInstanceOf<DamageGroupTrigger>(trigger.Triggers[1]);

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Change triggers once to true
                threshold.TriggersOnce = true;

                // Heal brute and burn back to 0
                sDamageableSystem.SetAllDamage(sDamageableComponent, 0);

                // No new thresholds reached from healing
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise brute damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bruteDamage * 2, true);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise burn damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, burnDamage * 2, true);

                // No new thresholds reached as triggers once is set to true and it already triggered before
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
