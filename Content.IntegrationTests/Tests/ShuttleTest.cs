using System.Threading.Tasks;
using Content.Server.Shuttles.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

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
            var entManager = server.ResolveDependency<IEntityManager>();
            var physicsSystem = entManager.System<SharedPhysicsSystem>();

            EntityUid gridEnt = default;

            await server.WaitAssertion(() =>
            {
                var mapId = mapMan.CreateMap();
                var grid = mapMan.CreateGrid(mapId);
                gridEnt = grid.Owner;

                Assert.That(entManager.HasComponent<ShuttleComponent>(gridEnt));
                Assert.That(entManager.TryGetComponent<PhysicsComponent>(gridEnt, out var physicsComponent));
                Assert.That(physicsComponent!.BodyType, Is.EqualTo(BodyType.Dynamic));
                Assert.That(entManager.GetComponent<TransformComponent>(gridEnt).LocalPosition, Is.EqualTo(Vector2.Zero));
                physicsSystem.ApplyLinearImpulse(gridEnt, Vector2.One, body: physicsComponent);
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That(entManager.GetComponent<TransformComponent>(gridEnt).LocalPosition, Is.Not.EqualTo(Vector2.Zero));
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
