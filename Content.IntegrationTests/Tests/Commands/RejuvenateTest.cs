using System.Threading.Tasks;
using Content.Server.GlobalVerbs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(RejuvenateVerb))]
    public class RejuvenateTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: DamageableDummy
  id: DamageableDummy
  components:
  - type: Damageable
    damagePrototype: biologicalDamageContainer
  - type: MobState
    thresholds:
      0: !type:NormalMobState {}
      100: !type:CriticalMobState {}
      200: !type:DeadMobState {}
";

        [Test]
        public async Task RejuvenateDeadTest()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                var human = entityManager.SpawnEntity("DamageableDummy", MapCoordinates.Nullspace);

                // Sanity check
                Assert.True(human.TryGetComponent(out IDamageableComponent damageable));
                Assert.True(human.TryGetComponent(out IMobStateComponent mobState));
                Assert.That(mobState.IsAlive, Is.True);
                Assert.That(mobState.IsCritical, Is.False);
                Assert.That(mobState.IsDead, Is.False);
                Assert.That(mobState.IsIncapacitated, Is.False);

                // Kill the entity
                damageable.ChangeDamage(DamageClass.Brute, 10000000, true);

                // Check that it is dead
                Assert.That(mobState.IsAlive, Is.False);
                Assert.That(mobState.IsCritical, Is.False);
                Assert.That(mobState.IsDead, Is.True);
                Assert.That(mobState.IsIncapacitated, Is.True);

                // Rejuvenate them
                RejuvenateVerb.PerformRejuvenate(human);

                // Check that it is alive and with no damage
                Assert.That(mobState.IsAlive, Is.True);
                Assert.That(mobState.IsCritical, Is.False);
                Assert.That(mobState.IsDead, Is.False);
                Assert.That(mobState.IsIncapacitated, Is.False);

                Assert.That(damageable.TotalDamage, Is.Zero);
            });
        }
    }
}
