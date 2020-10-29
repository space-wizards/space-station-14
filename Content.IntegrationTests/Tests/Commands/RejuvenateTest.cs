using Content.Server.GlobalVerbs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
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
        public void RejuvenateDeadTest()
        {
            var server = StartServerDummyTicker();

            IEntity human = null;
            IDamageableComponent damageable = null;

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);

                // Sanity check
                Assert.True(human.TryGetComponent(out damageable));
                Assert.That(damageable.CurrentState, Is.EqualTo(DamageState.Alive));

                // Kill the entity
                damageable.ChangeDamage(DamageClass.Brute, 10000000, true);

                // Check that it is dead
                Assert.That(damageable.CurrentState, Is.EqualTo(DamageState.Dead));

                // Rejuvenate them
                RejuvenateVerb.PerformRejuvenate(human);

                // Check that it is alive and with no damage
                Assert.That(damageable.CurrentState, Is.EqualTo(DamageState.Alive));
                Assert.That(damageable.TotalDamage, Is.Zero);
            });
        }
    }
}
