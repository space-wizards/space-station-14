using System.Linq;
using System.Threading.Tasks;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible
{
    [TestFixture]
    [TestOf(typeof(DestructibleComponent))]
    [TestOf(typeof(Threshold))]
    public class DestructibleThresholdActivationTest : ContentIntegrationTest
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
            DestructibleComponent sDestructibleComponent = null;
            TestThresholdListenerComponent sThresholdListenerComponent = null;

            await server.WaitPost(() =>
            {
                var mapId = new MapId(1);
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleEntityId, coordinates);
                sDamageableComponent = sDestructibleEntity.GetComponent<IDamageableComponent>();
                sDestructibleComponent = sDestructibleEntity.GetComponent<DestructibleComponent>();
                sThresholdListenerComponent = sDestructibleEntity.GetComponent<TestThresholdListenerComponent>();
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);
            });

            await server.WaitAssertion(() =>
            {
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // No thresholds reached yet, the earliest one is at 20 damage
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // Only one threshold reached, 20
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold 20
                var msg = sThresholdListenerComponent.ThresholdsReached[0];
                var threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);

                sThresholdListenerComponent.ThresholdsReached.Clear();

                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 30, true));

                // One threshold reached, 50, since 20 already triggered before and it has not been healed below that amount
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                // Threshold 50
                msg = sThresholdListenerComponent.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                var soundThreshold = (PlaySoundBehavior) threshold.Behaviors[0];
                var spawnThreshold = (SpawnEntitiesBehavior) threshold.Behaviors[1];
                var actsThreshold = (DoActsBehavior) threshold.Behaviors[2];

                Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                Assert.That(soundThreshold.Sound, Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                Assert.That(spawnThreshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);

                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Damage for 50 again, up to 100 now
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 50, true));

                // No thresholds reached as they weren't healed below the trigger amount
                Assert.IsEmpty(sThresholdListenerComponent.ThresholdsReached);

                // Heal down to 0
                sDamageableComponent.Heal();

                // Damage for 100, up to 100
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 100, true));

                // Two thresholds reached as damage increased past the previous, 20 and 50
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(2));

                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Heal the entity for 40 damage, down to 60
                sDamageableComponent.ChangeDamage(DamageType.Blunt, -40, true);

                // Thresholds don't work backwards
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);

                // Damage for 10, up to 70
                sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true);

                // Not enough healing to de-trigger a threshold
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);

                // Heal by 30, down to 40
                sDamageableComponent.ChangeDamage(DamageType.Blunt, -30, true);

                // Thresholds don't work backwards
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);

                // Damage up to 50 again
                sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true);

                // The 50 threshold should have triggered again, after being healed
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                msg = sThresholdListenerComponent.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                soundThreshold = (PlaySoundBehavior) threshold.Behaviors[0];
                spawnThreshold = (SpawnEntitiesBehavior) threshold.Behaviors[1];
                actsThreshold = (DoActsBehavior) threshold.Behaviors[2];

                // Check that it matches the YAML prototype
                Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                Assert.That(soundThreshold.Sound, Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                Assert.That(spawnThreshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);

                // Reset thresholds reached
                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Heal all damage
                sDamageableComponent.Heal();

                // Damage up to 50
                sDamageableComponent.ChangeDamage(DamageType.Blunt, 50, true);

                // Check that the total damage matches
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(50));

                // Both thresholds should have triggered
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Has.Exactly(2).Items);

                // Verify the first one, should be the lowest one (20)
                msg = sThresholdListenerComponent.ThresholdsReached[0];
                var trigger = (DamageTrigger) msg.Threshold.Trigger;
                Assert.NotNull(trigger);
                Assert.That(trigger.Damage, Is.EqualTo(20));

                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);

                // Verify the second one, should be the highest one (50)
                msg = sThresholdListenerComponent.ThresholdsReached[1];
                trigger = (DamageTrigger) msg.Threshold.Trigger;
                Assert.NotNull(trigger);
                Assert.That(trigger.Damage, Is.EqualTo(50));

                threshold = msg.Threshold;

                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                soundThreshold = (PlaySoundBehavior) threshold.Behaviors[0];
                spawnThreshold = (SpawnEntitiesBehavior) threshold.Behaviors[1];
                actsThreshold = (DoActsBehavior) threshold.Behaviors[2];

                // Check that it matches the YAML prototype
                Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                Assert.That(soundThreshold.Sound, Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                Assert.That(spawnThreshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.NotNull(threshold.Trigger);
                Assert.That(threshold.Triggered, Is.True);

                // Reset thresholds reached
                sThresholdListenerComponent.ThresholdsReached.Clear();

                // Heal the entity completely
                sDamageableComponent.Heal();

                // Check that the entity has 0 damage
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(0));

                // Set both thresholds to only trigger once
                foreach (var destructibleThreshold in sDestructibleComponent.Thresholds)
                {
                    Assert.NotNull(destructibleThreshold.Trigger);
                    destructibleThreshold.TriggersOnce = true;
                }

                // Damage the entity up to 50 damage again
                sDamageableComponent.ChangeDamage(DamageType.Blunt, 50, true);

                // Check that the total damage matches
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(50));

                // No thresholds should have triggered as they were already triggered before, and they are set to only trigger once
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);

                // Set both thresholds to trigger multiple times
                foreach (var destructibleThreshold in sDestructibleComponent.Thresholds)
                {
                    Assert.NotNull(destructibleThreshold.Trigger);
                    destructibleThreshold.TriggersOnce = false;
                }

                // Check that the total damage matches
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(50));

                // They shouldn't have been triggered by changing TriggersOnce
                Assert.That(sThresholdListenerComponent.ThresholdsReached, Is.Empty);
            });
        }
    }
}
