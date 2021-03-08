using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible
{
    [TestFixture]
    [TestOf(typeof(DamageTypeTrigger))]
    [TestOf(typeof(AndTrigger))]
    public class DestructibleDamageTypeTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes,
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IComponentFactory>().RegisterClass<TestThresholdListenerComponent>();
                }
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();

            IEntity sDestructibleEntity;
            IDamageableComponent sDamageableComponent = null;
            TestThresholdListenerComponent sThresholdListenerComponent = null;

            await server.WaitPost(() =>
            {
                var mapId = new MapId(1);
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleDamageTypeEntityId, coordinates);
                sDamageableComponent = sDestructibleEntity.GetComponent<IDamageableComponent>();
                sThresholdListenerComponent = sDestructibleEntity.GetComponent<TestThresholdListenerComponent>();
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);
            });

            await server.WaitAssertion(() =>
            {
                // Raise blunt damage to 5
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 5, true));

                // No thresholds reached yet, the earliest one is at 10 damage
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Raise blunt damage to 10
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 5, true));

                // No threshold reached, slash needs to be 10 as well
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Raise slash damage to 10
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Slash, 10, true));

                // One threshold reached, blunt 10 + slash 10
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold blunt 10 + slash 10
                var msg = sThresholdListenerComponent.ThresholdsReached[0];
                var threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);
                Assert.IsInstanceOf<AndTrigger>(threshold.Trigger);

                var trigger = (AndTrigger) threshold.Trigger;

                Assert.IsInstanceOf<DamageTypeTrigger>(trigger.Triggers[0]);
                Assert.IsInstanceOf<DamageTypeTrigger>(trigger.Triggers[1]);

                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Raise blunt damage to 20
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // No new thresholds reached
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Raise slash damage to 20
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Slash, 10, true));

                // No new thresholds reached
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Lower blunt damage to 0
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, -20, true));

                // No new thresholds reached, healing should not trigger it
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Raise blunt damage back up to 10
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // 10 blunt + 10 slash threshold reached, blunt was healed and brought back to its threshold amount and slash stayed the same
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Heal both types of damage to 0
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, -10, true));
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Slash, -20, true));

                // No new thresholds reached, healing should not trigger it
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Raise blunt damage to 10
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // No new thresholds reached
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Raise slash damage to 10
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Slash, 10, true));

                // Both types of damage were healed and then raised again, the threshold should have been reached as triggers once is default false
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold blunt 10 + slash 10
                msg = sThresholdListenerComponent.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);
                Assert.IsInstanceOf<AndTrigger>(threshold.Trigger);

                trigger = (AndTrigger) threshold.Trigger;

                Assert.IsInstanceOf<DamageTypeTrigger>(trigger.Triggers[0]);
                Assert.IsInstanceOf<DamageTypeTrigger>(trigger.Triggers[1]);

                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Change triggers once to true
                threshold.TriggersOnce = true;

                // Heal blunt and slash back to 0
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, -10, true));
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Slash, -10, true));

                // No new thresholds reached from healing
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Raise blunt damage to 10
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // No new thresholds reached
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Raise slash damage to 10
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Slash, 10, true));

                // No new thresholds reached as triggers once is set to true and it already triggered before
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);
            });
        }
    }
}
