using System.Threading.Tasks;
using Content.Server.GlobalVerbs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(RejuvenateVerb))]
    public class RejuvenateTest : ContentIntegrationTest
    {
        [Test]
        public async Task RejuvenateDeadTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                var human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);

                // Sanity check
                Assert.True(human.TryGetComponent(out IDamageableComponent damageable));
                Assert.True(human.TryGetComponent(out SharedMobStateComponent mobState));
                Assert.That(mobState.IsAlive);
                Assert.That(mobState.IsCritical, Is.False);
                Assert.That(mobState.IsDead, Is.False);

                // Kill the entity
                damageable.ChangeDamage(DamageClass.Brute, 10000000, true);

                // Check that it is dead
                Assert.That(mobState.IsAlive, Is.False);
                Assert.That(mobState.IsClientSide, Is.False);
                Assert.That(mobState.IsDead);

                // Rejuvenate them
                RejuvenateVerb.PerformRejuvenate(human);

                // Check that it is alive and with no damage
                Assert.That(mobState.IsAlive);
                Assert.That(mobState.IsCritical, Is.False);
                Assert.That(mobState.IsDead, Is.False);
                Assert.That(damageable.TotalDamage, Is.Zero);
            });
        }
    }
}
