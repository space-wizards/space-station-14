using System.Threading.Tasks;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Gravity
{
    [TestFixture]
    [TestOf(typeof(WeightlessStatusSystem))]
    [TestOf(typeof(WeightlessChangeMessage))]
    public class WeightlessStatusTests : ContentIntegrationTest
    {
        [Test]
        public async Task WeightlessStatusTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                var human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);

               // Sanity checks
                Assert.True(human.TryGetComponent(out SharedStatusEffectsComponent statusEffects));

                human.IsWeightless();

                // No gravity in null space
                Assert.True(statusEffects.Statuses.ContainsKey(StatusEffect.Weightless));
            });
        }
    }
}
