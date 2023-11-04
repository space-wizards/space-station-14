using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameObjects;
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
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var testMap = await pair.CreateTestMap();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sEntitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();

            EntityUid sDestructibleEntity = default;
            DamageableComponent sDamageableComponent = null;
            TestDestructibleListenerSystem sTestThresholdListenerSystem = null;
            DamageableSystem sDamageableSystem = null;

            await server.WaitPost(() =>
            {
                var coordinates = testMap.GridCoords;

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleDamageTypeEntityId, coordinates);
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
                var bluntDamageType = protoManager.Index<DamageTypePrototype>("TestBlunt");
                var slashDamageType = protoManager.Index<DamageTypePrototype>("TestSlash");

                var bluntDamage = new DamageSpecifier(bluntDamageType, 5);
                var slashDamage = new DamageSpecifier(slashDamageType, 5);

                // Raise blunt damage to 5
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage, true);

                // No thresholds reached yet, the earliest one is at 10 damage
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise blunt damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage, true);

                // No threshold reached, slash needs to be 10 as well
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise slash damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * 2, true);

                // One threshold reached, blunt 10 + slash 10
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));

                // Threshold blunt 10 + slash 10
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

                var trigger = (AndTrigger) threshold.Trigger;

                Assert.Multiple(() =>
                {
                    Assert.That(trigger.Triggers[0], Is.InstanceOf<DamageTypeTrigger>());
                    Assert.That(trigger.Triggers[1], Is.InstanceOf<DamageTypeTrigger>());
                });

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Raise blunt damage to 20
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 2, true);

                // No new thresholds reached
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise slash damage to 20
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * 2, true);

                // No new thresholds reached
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Lower blunt damage to 0
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * -4, true);

                // No new thresholds reached, healing should not trigger it
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise blunt damage back up to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 2, true);

                // 10 blunt + 10 slash threshold reached, blunt was healed and brought back to its threshold amount and slash stayed the same
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal both types of damage to 0
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * -2, true);
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * -4, true);

                // No new thresholds reached, healing should not trigger it
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise blunt damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 2, true);

                // No new thresholds reached
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise slash damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * 2, true);

                // Both types of damage were healed and then raised again, the threshold should have been reached as triggers once is default false
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));

                // Threshold blunt 10 + slash 10
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

                trigger = (AndTrigger) threshold.Trigger;

                Assert.Multiple(() =>
                {
                    Assert.That(trigger.Triggers[0], Is.InstanceOf<DamageTypeTrigger>());
                    Assert.That(trigger.Triggers[1], Is.InstanceOf<DamageTypeTrigger>());
                });

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Change triggers once to true
                threshold.TriggersOnce = true;

                // Heal blunt and slash back to 0
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * -2, true);
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * -2, true);

                // No new thresholds reached from healing
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise blunt damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 2, true);

                // No new thresholds reached
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Raise slash damage to 10
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, slashDamage * 2, true);

                // No new thresholds reached as triggers once is set to true and it already triggered before
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);
            });
            await pair.CleanReturnAsync();
        }
    }
}
