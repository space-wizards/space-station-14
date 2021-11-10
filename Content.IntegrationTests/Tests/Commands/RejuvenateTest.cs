using System.Threading.Tasks;
using Content.Server.Administration.Commands;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(RejuvenateCommand))]
    public class RejuvenateTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: DamageableDummy
  id: DamageableDummy
  components:
  - type: Damageable
    damageContainer: Biological
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
            var server = StartServer(options);

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

                var human = entityManager.SpawnEntity("DamageableDummy", MapCoordinates.Nullspace);

                // Sanity check
                Assert.True(human.TryGetComponent(out DamageableComponent damageable));
                Assert.True(human.TryGetComponent(out MobStateComponent mobState));
                mobState.UpdateState(0);
                Assert.That(mobState.IsAlive, Is.True);
                Assert.That(mobState.IsCritical, Is.False);
                Assert.That(mobState.IsDead, Is.False);
                Assert.That(mobState.IsIncapacitated, Is.False);

                // Kill the entity
                DamageSpecifier damage = new(prototypeManager.Index<DamageGroupPrototype>("Toxin"),
                    FixedPoint2.New(10000000));
                EntitySystem.Get<DamageableSystem>().TryChangeDamage(human.Uid, damage, true);

                // Check that it is dead
                Assert.That(mobState.IsAlive, Is.False);
                Assert.That(mobState.IsCritical, Is.False);
                Assert.That(mobState.IsDead, Is.True);
                Assert.That(mobState.IsIncapacitated, Is.True);

                // Rejuvenate them
                RejuvenateCommand.PerformRejuvenate(human);

                // Check that it is alive and with no damage
                Assert.That(mobState.IsAlive, Is.True);
                Assert.That(mobState.IsCritical, Is.False);
                Assert.That(mobState.IsDead, Is.False);
                Assert.That(mobState.IsIncapacitated, Is.False);

                Assert.That(damageable.TotalDamage, Is.EqualTo(FixedPoint2.Zero));
            });
        }
    }
}
