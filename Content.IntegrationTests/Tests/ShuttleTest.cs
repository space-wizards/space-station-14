#nullable enable
using System.Threading.Tasks;
using Content.Server.Shuttles;
using Content.Server.Shuttles.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class ShuttleTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var entMan = server.ResolveDependency<IEntityManager>();
            var mapMan = server.ResolveDependency<IMapManager>();
            IEntity? gridEnt = null;

            await server.WaitAssertion(() =>
            {
                var mapId = mapMan.CreateMap();
                var grid = mapMan.CreateGrid(mapId);
                gridEnt = entMan.GetEntity(grid.GridEntityId);

                Assert.That(IoCManager.Resolve<IEntityManager>().TryGetComponent(gridEnt, out ShuttleComponent? shuttleComponent));
                Assert.That(IoCManager.Resolve<IEntityManager>().TryGetComponent(gridEnt, out PhysicsComponent? physicsComponent));
                Assert.That(physicsComponent!.BodyType, Is.EqualTo(BodyType.Dynamic));
                Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt).LocalPosition, Is.EqualTo(Vector2.Zero));
                physicsComponent.ApplyLinearImpulse(Vector2.One);
            });

            // TODO: Should have tests that collision + rendertree + pointlights work on a moved grid but I'll deal with that
            // when we get rotations.
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That<Vector2?>((gridEnt != null ? IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt) : null).LocalPosition, Is.Not.EqualTo(Vector2.Zero));
            });
        }
    }
}
