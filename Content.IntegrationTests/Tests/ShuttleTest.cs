using System.Threading.Tasks;
using Content.Server.Shuttles.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class ShuttleTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var mapMan = server.ResolveDependency<IMapManager>();
            var sEntities = server.ResolveDependency<IEntityManager>();

            EntityUid gridEnt = default;

            await server.WaitAssertion(() =>
            {
                var mapId = mapMan.CreateMap();
                var grid = mapMan.CreateGrid(mapId);
                gridEnt = grid.GridEntityId;

                Assert.That(sEntities.HasComponent<ShuttleComponent>(gridEnt));
                Assert.That(sEntities.TryGetComponent<PhysicsComponent>(gridEnt, out var physicsComponent));
                Assert.That(physicsComponent!.BodyType, Is.EqualTo(BodyType.Dynamic));
                Assert.That(sEntities.GetComponent<TransformComponent>(gridEnt).LocalPosition, Is.EqualTo(Vector2.Zero));
                physicsComponent.ApplyLinearImpulse(Vector2.One);
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That<Vector2?>(sEntities.GetComponent<TransformComponent>(gridEnt).LocalPosition, Is.Not.EqualTo(Vector2.Zero));
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
