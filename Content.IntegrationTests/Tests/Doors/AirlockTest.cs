using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Doors;
using NUnit.Framework;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using static Content.Server.GameObjects.Components.Doors.ServerDoorComponent;

namespace Content.IntegrationTests.Tests.Doors
{
    [TestFixture]
    [TestOf(typeof(AirlockComponent))]
    public class AirlockTest : ContentIntegrationTest
    {
        [Test]
        public async Task OpenCloseDestroyTest()
        {
            var server = StartServerDummyTicker();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            IEntity airlock = null;
            AirlockComponent airlockComponent = null;

            server.Assert(() =>
            {
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                airlock = entityManager.SpawnEntity("Airlock", MapCoordinates.Nullspace);

                Assert.True(airlock.TryGetComponent(out airlockComponent));
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closed));
            });

            await server.WaitIdleAsync();

            server.Assert(() =>
            {
                airlockComponent.Open();
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Opening));
            });

            await server.WaitIdleAsync();

            await WaitUntil(server, _ => airlockComponent.State == DoorState.Open);

            Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Open));

            server.Assert(() =>
            {
                airlockComponent.Close();
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closing));
            });

            await WaitUntil(server, _ => airlockComponent.State == DoorState.Closed);

            Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closed));

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    airlock.Delete();
                });
            });

            server.RunTicks(5);

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task AirlockBlockTest()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            IEntity human = null;
            IEntity airlock = null;
            TestController controller = null;
            AirlockComponent airlockComponent = null;

            var humanStartingX = -1;

            server.Assert(() =>
            {
                var mapId = new MapId(1);
                mapManager.CreateNewMapEntity(mapId);

                var humanCoordinates = new MapCoordinates((humanStartingX, 0), mapId);
                human = entityManager.SpawnEntity("HumanMob_Content", humanCoordinates);

                airlock = entityManager.SpawnEntity("Airlock", new MapCoordinates((0, 0), mapId));

                Assert.True(human.TryGetComponent(out ICollidableComponent collidable));

                controller = collidable.EnsureController<TestController>();

                Assert.True(airlock.TryGetComponent(out airlockComponent));
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closed));
            });

            await server.WaitIdleAsync();

            // Push the human towards the airlock
            controller.LinearVelocity = (0.5f, 0);

            for (var i = 0; i < 240; i += 10)
            {
                // Keep the airlock awake so they collide
                airlock.GetComponent<ICollidableComponent>().WakeBody();

                // Ensure that it is still closed
                Assert.That(airlockComponent.State, Is.EqualTo(DoorState.Closed));

                await server.WaitRunTicks(10);
                await server.WaitIdleAsync();
            }

            // Sanity check
            Assert.That(human.Transform.MapPosition.X, Is.GreaterThan(humanStartingX));

            // Blocked by the airlock
            Assert.That(human.Transform.MapPosition.X, Is.Negative.Or.Zero);
        }

        private class TestController : VirtualController { }
    }
}
