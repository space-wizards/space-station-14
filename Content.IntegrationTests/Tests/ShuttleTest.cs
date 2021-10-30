#nullable enable
using System.Threading.Tasks;
using Content.Server.Shuttles;
using NUnit.Framework;
using Robust.Shared.GameObjects;
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

                Assert.That(gridEnt.TryGetComponent(out ShuttleComponent? shuttleComponent));
                Assert.That(gridEnt.TryGetComponent(out PhysicsComponent? physicsComponent));
                Assert.That(physicsComponent!.BodyType, Is.EqualTo(BodyType.Dynamic));
                Assert.That(gridEnt.Transform.LocalPosition, Is.EqualTo(Vector2.Zero));
                physicsComponent.ApplyLinearImpulse(Vector2.One);
            });

            // TODO: Should have tests that collision + rendertree + pointlights work on a moved grid but I'll deal with that
            // when we get rotations.
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That(gridEnt?.Transform.LocalPosition, Is.Not.EqualTo(Vector2.Zero));
            });
        }
    }
}
