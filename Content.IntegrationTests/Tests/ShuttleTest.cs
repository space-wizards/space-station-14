using System.Numerics;
using Content.Server.Shuttles.Components;
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
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            await server.WaitIdleAsync();

            var mapMan = server.ResolveDependency<IMapManager>();
            var entManager = server.ResolveDependency<IEntityManager>();
            var physicsSystem = entManager.System<SharedPhysicsSystem>();

            EntityUid gridEnt = default;
            PhysicsComponent gridPhys = null;

            await server.WaitAssertion(() =>
            {
                var mapId = mapMan.CreateMap();
                var grid = mapMan.CreateGrid(mapId);
                gridEnt = grid.Owner;

                Assert.Multiple(() =>
                {
                    Assert.That(entManager.HasComponent<ShuttleComponent>(gridEnt));
                    Assert.That(entManager.TryGetComponent(gridEnt, out gridPhys));
                });
                Assert.Multiple(() =>
                {
                    Assert.That(gridPhys.BodyType, Is.EqualTo(BodyType.Dynamic));
                    Assert.That(entManager.GetComponent<TransformComponent>(gridEnt).LocalPosition, Is.EqualTo(Vector2.Zero));
                });
                physicsSystem.ApplyLinearImpulse(gridEnt, Vector2.One, body: gridPhys);
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That(entManager.GetComponent<TransformComponent>(gridEnt).LocalPosition, Is.Not.EqualTo(Vector2.Zero));
            });
            await pair.CleanReturnAsync();
        }
    }
}
