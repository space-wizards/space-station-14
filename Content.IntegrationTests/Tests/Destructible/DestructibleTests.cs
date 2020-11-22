using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Destructible;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Destructible
{
    [TestFixture]
    [TestOf(typeof(DestructibleComponent))]
    [TestOf(typeof(Threshold))]
    public class DestructibleTests : ContentIntegrationTest
    {
        private static readonly string DestructibleEntityId = "DestructibleTestsDestructibleEntity";

        private static readonly string Prototypes = $@"
- type: entity
  id: {DestructibleEntityId}
  name: {DestructibleEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
      20: {{}}
      50:
        Sound: /Audio/Effects/woodhit.ogg
        Spawn:
          WoodPlank:
            Min: 1
            Max: 1
        Acts: [""Destruction""]
  - type: TestThresholdListener
";

        private class TestThresholdListenerComponent : Component
        {
            public override string Name => "TestThresholdListener";

            public List<DestructibleThresholdReachedMessage> ThresholdsReached { get; } = new List<DestructibleThresholdReachedMessage>();

            public override void HandleMessage(ComponentMessage message, IComponent component)
            {
                base.HandleMessage(message, component);

                switch (message)
                {
                    case DestructibleThresholdReachedMessage msg:
                        ThresholdsReached.Add(msg);
                        break;
                }
            }
        }

        [Test]
        public async Task TestThresholdActivation()
        {
            var server = StartServerDummyTicker(new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes,
                ContentBeforeIoC = () =>
                {
                    IoCManager.Resolve<IComponentFactory>().Register<TestThresholdListenerComponent>();
                }
            });

            await server.WaitIdleAsync();

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();

            IEntity sDestructibleEntity = null;
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
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.Zero);
            });

            await server.WaitAssertion(() =>
            {
                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // No thresholds reached yet, the earliest one is at 20 damage
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.Zero);

                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 10, true));

                // Only one threshold reached, 20
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                var msg = sThresholdListenerComponent.ThresholdsReached[0];

                // Check that it matches the total damage dealt
                Assert.That(msg.ThresholdAmount, Is.EqualTo(20));

                var threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Acts, Is.EqualTo(0));
                Assert.That(threshold.Sound, Is.Null.Or.Empty);
                Assert.That(threshold.Spawn, Is.Null);
                Assert.That(threshold.SoundCollection, Is.Null.Or.Empty);
                Assert.That(threshold.Triggered, Is.True);

                sThresholdListenerComponent.ThresholdsReached.Clear();

                Assert.True(sDamageableComponent.ChangeDamage(DamageType.Blunt, 30, true));

                // Only one threshold reached, 50, since 20 was already reached before
                Assert.That(sThresholdListenerComponent.ThresholdsReached.Count, Is.EqualTo(1));

                msg = sThresholdListenerComponent.ThresholdsReached[0];

                // Check that it matches the total damage dealt
                Assert.That(msg.ThresholdAmount, Is.EqualTo(50));

                threshold = msg.Threshold;

                // Check that it matches the YAML prototype
                Assert.That(threshold.Acts, Is.EqualTo((int) ThresholdActs.Destruction));
                Assert.That(threshold.Sound, Is.EqualTo("/Audio/Effects/woodhit.ogg"));
                Assert.That(threshold.Spawn, Is.Not.Null);
                Assert.That(threshold.Spawn.Count, Is.EqualTo(1));
                Assert.That(threshold.Spawn.Single().Key, Is.EqualTo("WoodPlank"));
                Assert.That(threshold.Spawn.Single().Value.Min, Is.EqualTo(1));
                Assert.That(threshold.Spawn.Single().Value.Max, Is.EqualTo(1));
                Assert.That(threshold.SoundCollection, Is.Null.Or.Empty);
                Assert.That(threshold.Triggered, Is.True);
            });
        }
    }
}
