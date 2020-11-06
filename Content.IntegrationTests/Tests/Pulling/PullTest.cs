using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.Physics.Pull;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Pulling
{
    [TestFixture]
    [TestOf(typeof(SharedPullableComponent))]
    [TestOf(typeof(SharedPullerComponent))]
    [TestOf(typeof(PullController))]
    public class PullTest : ContentIntegrationTest
    {
        [Test]
        public async Task AnchoredNoPullTest()
        {
            var server = StartServerDummyTicker();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                var chair = entityManager.SpawnEntity("ChairWood", MapCoordinates.Nullspace);

                var puller = human.EnsureComponent<SharedPullerComponent>();
                var pullable = chair.EnsureComponent<PullableComponent>();
                var pullablePhysics = chair.GetComponent<PhysicsComponent>();

                pullablePhysics.Anchored = false;

                Assert.That(pullable.TryStartPull(puller.Owner));
                Assert.That(pullable.Puller, Is.EqualTo(puller.Owner));
                Assert.That(pullable.BeingPulled);

                Assert.That(puller.Pulling, Is.EqualTo(pullable.Owner));

                Assert.That(pullable.TryStopPull);
                Assert.That(pullable.Puller, Is.Null);
                Assert.That(pullable.BeingPulled, Is.False);

                Assert.That(puller.Pulling, Is.Null);

                pullablePhysics.Anchored = true;

                Assert.That(pullable.TryStartPull(puller.Owner), Is.False);
                Assert.That(pullable.Puller, Is.Null);
                Assert.That(pullable.BeingPulled, Is.False);

                Assert.That(puller.Pulling, Is.Null);
            });
        }
    }
}
