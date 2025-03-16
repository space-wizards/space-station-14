using Content.Server.Administration.Commands;
using Content.Server.Administration.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(RejuvenateSystem))]
    public sealed class RejuvenateTest
    {
        [TestPrototypes]
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
      200: Dead
";

        [Test]
        public async Task RejuvenateDeadTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mobStateSystem = entManager.System<MobStateSystem>();
            var damSystem = entManager.System<DamageableSystem>();
            var rejuvenateSystem = entManager.System<RejuvenateSystem>();

            await server.WaitAssertion(() =>
            {
                var human = entManager.SpawnEntity("DamageableDummy", MapCoordinates.Nullspace);
                DamageableComponent damageable = null;
                MobStateComponent mobState = null;

                // Sanity check
                Assert.Multiple(() =>
                {
                    Assert.That(entManager.TryGetComponent(human, out damageable));
                    Assert.That(entManager.TryGetComponent(human, out mobState));
                });
                Assert.Multiple(() =>
                {
                    Assert.That(mobStateSystem.IsAlive(human, mobState), Is.True);
                    Assert.That(mobStateSystem.IsCritical(human, mobState), Is.False);
                    Assert.That(mobStateSystem.IsDead(human, mobState), Is.False);
                    Assert.That(mobStateSystem.IsIncapacitated(human, mobState), Is.False);
                });

                // Kill the entity
                DamageSpecifier damage = new(prototypeManager.Index<DamageGroupPrototype>("Toxin"), FixedPoint2.New(10000000));

                damSystem.TryChangeDamage(human, damage, true);

                // Check that it is dead
                Assert.Multiple(() =>
                {
                    Assert.That(mobStateSystem.IsAlive(human, mobState), Is.False);
                    Assert.That(mobStateSystem.IsCritical(human, mobState), Is.False);
                    Assert.That(mobStateSystem.IsDead(human, mobState), Is.True);
                    Assert.That(mobStateSystem.IsIncapacitated(human, mobState), Is.True);
                });

                // Rejuvenate them
                rejuvenateSystem.PerformRejuvenate(human);

                // Check that it is alive and with no damage
                Assert.Multiple(() =>
                {
                    Assert.That(mobStateSystem.IsAlive(human, mobState), Is.True);
                    Assert.That(mobStateSystem.IsCritical(human, mobState), Is.False);
                    Assert.That(mobStateSystem.IsDead(human, mobState), Is.False);
                    Assert.That(mobStateSystem.IsIncapacitated(human, mobState), Is.False);

                    Assert.That(damageable.TotalDamage, Is.EqualTo(FixedPoint2.Zero));
                });
            });
            await pair.CleanReturnAsync();
        }
    }
}
