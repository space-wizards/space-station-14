using System.Threading.Tasks;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible
{
    [TestFixture]
    [TestOf(typeof(DamageTypeTrigger))]
    [TestOf(typeof(AndTrigger))]
    public sealed class DestructibleDamageTypeTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sEntitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            EntityUid sDestructibleEntity = default;
            DamageableComponent sDamageableComponent = null;
            TestDestructibleListenerSystem sTestThresholdListenerSystem = null;
            DamageableSystem sDamageableSystem = null;

            await server.WaitPost(() =>
            {
                var coordinates = testMap.GridCoords;

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleDamageTypeEntityId, coordinates);
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
                var bluntDamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>("TestBlunt");
                var slashDamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>("TestSlash");

                var bluntDamage = new DamageSpecifier(bluntDamageType,5);
                var slashDamage = new DamageSpecifier(slashDamageType,5);

                // Raise blunt damage to 5
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage, true);

                // No thresholds reached yet, the earliest one is at 10 damage
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise blunt damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage, true);

                // No threshold reached, slash needs to be 10 as well
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise slash damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * 2, true);

                // One threshold reached, blunt 10 + slash 10
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold blunt 10 + slash 10
                var msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                var threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);
                Assert.IsInstanceOf<AndTrigger>(threshold.Trigger);

                var trigger = (AndTrigger) threshold.Trigger;

                Assert.IsInstanceOf<DamageTypeTrigger>(trigger.Triggers[0]);
                Assert.IsInstanceOf<DamageTypeTrigger>(trigger.Triggers[1]);

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Raise blunt damage to 20
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 2, true);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise slash damage to 20
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * 2, true);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Lower blunt damage to 0
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * -4, true);

                // No new thresholds reached, healing should not trigger it
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise blunt damage back up to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 2, true);

                // 10 blunt + 10 slash threshold reached, blunt was healed and brought back to its threshold amount and slash stayed the same
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal both types of damage to 0
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * -2, true);
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * -4, true);

                // No new thresholds reached, healing should not trigger it
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise blunt damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 2, true);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise slash damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * 2, true);

                // Both types of damage were healed and then raised again, the threshold should have been reached as triggers once is default false
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold blunt 10 + slash 10
                msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);
                Assert.IsInstanceOf<AndTrigger>(threshold.Trigger);

                trigger = (AndTrigger) threshold.Trigger;

                Assert.IsInstanceOf<DamageTypeTrigger>(trigger.Triggers[0]);
                Assert.IsInstanceOf<DamageTypeTrigger>(trigger.Triggers[1]);

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Change triggers once to true
                threshold.TriggersOnce = true;

                // Heal blunt and slash back to 0
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * -2, true);
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * -2, true);

                // No new thresholds reached from healing
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise blunt damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 2, true);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise slash damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * 2, true);

                // No new thresholds reached as triggers once is set to true and it already triggered before
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
