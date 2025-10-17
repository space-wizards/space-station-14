using System.Linq;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio.Systems;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using static Content.IntegrationTests.Tests.Destructible.DestructibleTestPrototypes;

namespace Content.IntegrationTests.Tests.Destructible
{
    [TestFixture]
    [TestOf(typeof(DestructibleComponent))]
    [TestOf(typeof(DamageThreshold))]
    public sealed class DestructibleThresholdActivationTest
    {
        [Test]
        public async Task Test()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var sEntitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var audio = sEntitySystemManager.GetEntitySystem<SharedAudioSystem>();

            var testMap = await pair.CreateTestMap();

            EntityUid sDestructibleEntity = default;
            DamageableComponent sDamageableComponent = null;
            DestructibleComponent sDestructibleComponent = null;
            TestDestructibleListenerSystem sTestThresholdListenerSystem = null;
            DamageableSystem sDamageableSystem = null;

            await server.WaitPost(() =>
            {
                var coordinates = testMap.GridCoords;

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleEntityId, coordinates);
                sDamageableComponent = sEntityManager.GetComponent<DamageableComponent>(sDestructibleEntity);
                sDestructibleComponent = sEntityManager.GetComponent<DestructibleComponent>(sDestructibleEntity);

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
                var bluntDamage = new DamageSpecifier(sPrototypeManager.Index<DamageTypePrototype>(TestBluntDamageTypeId), 10);

                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage, true);

                // No thresholds reached yet, the earliest one is at 20 damage
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage, true);

                // Only one threshold reached, 20
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));

                // Threshold 20
                var msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                var threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.Multiple(() =>
                {
                    Assert.That(threshold.Behaviors, Is.Empty);
                    Assert.That(threshold.Trigger, Is.Not.Null);
                    Assert.That(threshold.Triggered, Is.True);
                });

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 3, true);

                // One threshold reached, 50, since 20 already triggered before and it has not been healed below that amount
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));

                // Threshold 50
                msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                var soundThreshold = (PlaySoundBehavior)threshold.Behaviors[0];
                var spawnThreshold = (SpawnEntitiesBehavior)threshold.Behaviors[1];
                var actsThreshold = (DoActsBehavior)threshold.Behaviors[2];

                Assert.Multiple(() =>
                {
                    Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                    Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                    Assert.That(spawnThreshold.Spawn, Has.Count.EqualTo(1));
                    Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                    Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                    Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                    Assert.That(threshold.Trigger, Is.Not.Null);
                    Assert.That(threshold.Triggered, Is.True);
                });

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Damage for 50 again, up to 100 now
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 5, true);

                // No thresholds reached as they weren't healed below the trigger amount
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Set damage to 0
                sDamageableSystem.SetAllDamage(sDestructibleEntity, sDamageableComponent, 0);

                // Damage for 100, up to 100
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 10, true);

                // Two thresholds reached as damage increased past the previous, 20 and 50
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(2));

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal the entity for 40 damage, down to 60
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * -4, true);

                // ThresholdsLookup don't work backwards
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Damage for 10, up to 70
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage, true);

                // Not enough healing to de-trigger a threshold
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Heal by 30, down to 40
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * -3, true);

                // ThresholdsLookup don't work backwards
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);

                // Damage up to 50 again
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage, true);

                // The 50 threshold should have triggered again, after being healed
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Count.EqualTo(1));

                msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                soundThreshold = (PlaySoundBehavior)threshold.Behaviors[0];
                spawnThreshold = (SpawnEntitiesBehavior)threshold.Behaviors[1];
                actsThreshold = (DoActsBehavior)threshold.Behaviors[2];

                // Check that it matches the YAML prototype
                Assert.Multiple(() =>
                {
                    Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                    Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                    Assert.That(spawnThreshold.Spawn, Has.Count.EqualTo(1));
                    Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                    Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                    Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                    Assert.That(threshold.Trigger, Is.Not.Null);
                    Assert.That(threshold.Triggered, Is.True);
                });

                // Reset thresholds reached
                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal all damage
                sDamageableSystem.SetAllDamage(sDestructibleEntity, sDamageableComponent, 0);

                // Damage up to 50
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 5, true);

                Assert.Multiple(() =>
                {
                    // Check that the total damage matches
                    Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(FixedPoint2.New(50)));

                    // Both thresholds should have triggered
                    Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Has.Exactly(2).Items);
                });

                // Verify the first one, should be the lowest one (20)
                msg = sTestThresholdListenerSystem.ThresholdsReached[0];
                var trigger = (DamageTrigger)msg.Threshold.Trigger;
                Assert.Multiple(() =>
                {
                    Assert.That(trigger, Is.Not.Null);
                    Assert.That(trigger.Damage, Is.EqualTo(FixedPoint2.New(20)));
                });

                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Behaviors, Is.Empty);

                // Verify the second one, should be the highest one (50)
                msg = sTestThresholdListenerSystem.ThresholdsReached[1];
                trigger = (DamageTrigger)msg.Threshold.Trigger;
                Assert.Multiple(() =>
                {
                    Assert.That(trigger, Is.Not.Null);
                    Assert.That(trigger.Damage, Is.EqualTo(FixedPoint2.New(50)));
                });

                threshold = msg.Threshold;

                Assert.That(threshold.Behaviors, Has.Count.EqualTo(3));

                soundThreshold = (PlaySoundBehavior)threshold.Behaviors[0];
                spawnThreshold = (SpawnEntitiesBehavior)threshold.Behaviors[1];
                actsThreshold = (DoActsBehavior)threshold.Behaviors[2];

                // Check that it matches the YAML prototype
                Assert.Multiple(() =>
                {
                    Assert.That(actsThreshold.Acts, Is.EqualTo(ThresholdActs.Breakage));
                    Assert.That(spawnThreshold.Spawn, Is.Not.Null);
                    Assert.That(spawnThreshold.Spawn, Has.Count.EqualTo(1));
                    Assert.That(spawnThreshold.Spawn.Single().Key, Is.EqualTo(SpawnedEntityId));
                    Assert.That(spawnThreshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                    Assert.That(spawnThreshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                    Assert.That(threshold.Trigger, Is.Not.Null);
                    Assert.That(threshold.Triggered, Is.True);
                });

                // Reset thresholds reached
                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal the entity completely
                sDamageableSystem.SetAllDamage(sDestructibleEntity, sDamageableComponent, 0);

                // Check that the entity has 0 damage
                Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(FixedPoint2.Zero));

                // Set both thresholds to only trigger once
                foreach (var destructibleThreshold in sDestructibleComponent.Thresholds)
                {
                    Assert.That(destructibleThreshold.Trigger, Is.Not.Null);
                    destructibleThreshold.TriggersOnce = true;
                }

                // Damage the entity up to 50 damage again
                sDamageableSystem.TryChangeDamage(sDestructibleEntity, bluntDamage * 5, true);

                Assert.Multiple(() =>
                {
                    // Check that the total damage matches
                    Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(FixedPoint2.New(50)));

                    // No thresholds should have triggered as they were already triggered before, and they are set to only trigger once
                    Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);
                });

                // Set both thresholds to trigger multiple times
                foreach (var destructibleThreshold in sDestructibleComponent.Thresholds)
                {
                    Assert.That(destructibleThreshold.Trigger, Is.Not.Null);
                    destructibleThreshold.TriggersOnce = false;
                }

                Assert.Multiple(() =>
                {
                    // Check that the total damage matches
                    Assert.That(sDamageableComponent.TotalDamage, Is.EqualTo(FixedPoint2.New(50)));

                    // They shouldn't have been triggered by changing TriggersOnce
                    Assert.That(sTestThresholdListenerSystem.ThresholdsReached, Is.Empty);
                });
            });
            await pair.CleanReturnAsync();
        }
    }
}
