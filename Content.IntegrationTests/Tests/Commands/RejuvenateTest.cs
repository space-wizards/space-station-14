using System.Threading.Tasks;
using Content.Server.Administration.Commands;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(RejuvenateCommand))]
    public sealed class RejuvenateTest
    {
        private const string Prototypes = @"
- type: entity
  name: DamageableDummy
  id: DamageableDummy
  components:
  - type: Damageable
    damageContainer: Biological
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Critical
      200: Dead
";

        [Test]
        public async Task RejuvenateDeadTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mobStateSystem = entManager.EntitySysManager.GetEntitySystem<MobStateSystem>();
            var damSystem = entManager.EntitySysManager.GetEntitySystem<DamageableSystem>();

            await server.WaitAssertion(() =>
            {
                var human = entManager.SpawnEntity("DamageableDummy", MapCoordinates.Nullspace);

                // Sanity check
                Assert.True(entManager.TryGetComponent(human, out DamageableComponent damageable));
                Assert.True(entManager.TryGetComponent(human, out MobStateComponent mobState));
                Assert.That(mobStateSystem.IsAlive(human, mobState), Is.True);
                Assert.That(mobStateSystem.IsCritical(human, mobState), Is.False);
                Assert.That(mobStateSystem.IsDead(human, mobState), Is.False);
                Assert.That(mobStateSystem.IsIncapacitated(human, mobState), Is.False);

                // Kill the entity
                DamageSpecifier damage = new(prototypeManager.Index<DamageGroupPrototype>("Toxin"),
                    FixedPoint2.New(10000000));

                damSystem.TryChangeDamage(human, damage, true);

                // Check that it is dead
                Assert.That(mobStateSystem.IsAlive(human, mobState), Is.False);
                Assert.That(mobStateSystem.IsCritical(human, mobState), Is.False);
                Assert.That(mobStateSystem.IsDead(human, mobState), Is.True);
                Assert.That(mobStateSystem.IsIncapacitated(human, mobState), Is.True);

                // Rejuvenate them
                RejuvenateCommand.PerformRejuvenate(human);

                // Check that it is alive and with no damage
                Assert.That(mobStateSystem.IsAlive(human, mobState), Is.True);
                Assert.That(mobStateSystem.IsCritical(human, mobState), Is.False);
                Assert.That(mobStateSystem.IsDead(human, mobState), Is.False);
                Assert.That(mobStateSystem.IsIncapacitated(human, mobState), Is.False);

                Assert.That(damageable.TotalDamage, Is.EqualTo(FixedPoint2.Zero));
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
