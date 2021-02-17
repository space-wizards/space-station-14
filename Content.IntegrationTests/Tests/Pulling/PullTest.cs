using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.Physics.Pull;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Pulling
{
    [TestFixture]
    [TestOf(typeof(SharedPullableComponent))]
    [TestOf(typeof(SharedPullerComponent))]
    [TestOf(typeof(PullController))]
    public class PullTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: PullTestPullerDummy
  id: PullTestPullerDummy
  components:
  - type: Puller
  - type: Physics

- type: entity
  name: PullTestPullableDummy
  id: PullTestPullableDummy
  components:
  - type: Pullable
  - type: Physics
";

        [Test]
        public async Task AnchoredNoPullTest()
        {
            var options = new ServerContentIntegrationOption {ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var pullerEntity = entityManager.SpawnEntity("PullTestPullerDummy", MapCoordinates.Nullspace);
                var pullableEntity = entityManager.SpawnEntity("PullTestPullableDummy", MapCoordinates.Nullspace);

                var puller = pullerEntity.GetComponent<SharedPullerComponent>();
                var pullable = pullableEntity.GetComponent<PullableComponent>();
                var pullablePhysics = pullableEntity.GetComponent<PhysicsComponent>();

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
