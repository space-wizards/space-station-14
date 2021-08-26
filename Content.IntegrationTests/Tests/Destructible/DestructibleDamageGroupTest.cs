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
    [TestOf(typeof(DamageGroupTrigger))]
    [TestOf(typeof(AndTrigger))]
    public class DestructibleDamageGroupTest : ContentIntegrationTest
    {
        [Test]
        public async Task AndTest()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();
            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            var sEntitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            IEntity sDestructibleEntity = null;
            DamageableComponent sDamageableComponent = null;
            DestructibleThresholdListenerSystem sTestThresholdListenerSystem = null;
            DamageableSystem sDamageableSystem = null;

            await server.WaitPost((System.Action)(() =>
            {
                var mapId = new MapId(1);
                var coordinates = new MapCoordinates(0, 0, mapId);
                sMapManager.CreateMap(mapId);

                sDestructibleEntity = sEntityManager.SpawnEntity(DestructibleDamageGroupEntityId, coordinates);
                sDamageableComponent = sDestructibleEntity.GetComponent<DamageableComponent>();
                sTestThresholdListenerSystem = sEntitySystemManager.GetEntitySystem<DestructibleThresholdListenerSystem>();
                sDamageableSystem = sEntitySystemManager.GetEntitySystem<DamageableSystem>();
            }));

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);
            });

            await server.WaitAssertion(() =>
            {
                var bruteDamageGroup = sPrototypeManager.Index<DamageGroupPrototype>("TestBrute");
                var burnDamageGroup = sPrototypeManager.Index<DamageGroupPrototype>("TestBurn");

                DamageData bruteDamage = new(bruteDamageGroup,5);
                DamageData burnDamage = new(burnDamageGroup,5);

                // Raise brute damage to 5
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(bruteDamage, true), false);

                // No thresholds reached yet, the earliest one is at 10 damage
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise brute damage to 10
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(bruteDamage, true), false);

                // No threshold reached, burn needs to be 10 as well
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise burn damage to 10
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(burnDamage * 2, true), false);

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
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(bruteDamage * 2, true), false);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise burn damage to 20
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(burnDamage * 2, true), false);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Lower brute damage to 0
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(bruteDamage * -10), false);
                Assert.That(sDamageableComponent.TotalDamage,Is.EqualTo(20));

                // No new thresholds reached, healing should not trigger it
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise brute damage back up to 10
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(bruteDamage * 2, true), false);

                // 10 brute + 10 burn threshold reached, brute was healed and brought back to its threshold amount and slash stayed the same
                Assert.That(sTestThresholdListenerSystem.ThresholdsReached.Count, Is.EqualTo(1));

                sTestThresholdListenerSystem.ThresholdsReached.Clear();

                // Heal both classes of damage to 0
                sDamageableSystem.SetAllDamage(sDamageableComponent, 0);

                // No new thresholds reached, healing should not trigger it
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise brute damage to 10
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(bruteDamage * 2, true), false);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise burn damage to 10
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(burnDamage * 2, true), false);

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
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(bruteDamage * 2, true), false);

                // No new thresholds reached
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);

                // Raise burn damage to 10
                sEntityManager.EventBus.RaiseLocalEvent(sDestructibleEntity.Uid, new TryChangeDamageEvent(burnDamage * 2, true), false);

                // No new thresholds reached as triggers once is set to true and it already triggered before
                Assert.IsEmpty(sTestThresholdListenerSystem.ThresholdsReached);
            });
        }
    }
}
