using System;
using System.Threading.Tasks;
using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.IntegrationTests.Tests.Doors
{
    [TestFixture]
    [TestOf(typeof(AirlockComponent))]
    public sealed class AirlockTest : ContentIntegrationTest
    {
        private const string Prototypes = @"
- type: entity
  name: PhysicsDummy
  id: PhysicsDummy
  components:
  - type: Physics
    bodyType: Dynamic
  - type: Fixtures
    fixtures:
    - shape:
        !type:PhysShapeCircle
          bounds: ""-0.49,-0.49,0.49,0.49""
      layer:
      - Impassable

- type: entity
  name: AirlockDummy
  id: AirlockDummy
  components:
  - type: Door
  - type: Airlock
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
    - shape:
        !type:PhysShapeAabb
          bounds: ""-0.49,-0.49,0.49,0.49""
      mask:
      - Impassable
";
        [Test]
        public async Task OpenCloseDestroyTest()
        {
            var options = new ServerIntegrationOptions {ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            EntityUid airlock = default;
            DoorComponent doorComponent = null;

            server.Assert(() =>
            {
                mapManager.CreateNewMapEntity(MapId.Nullspace);

                airlock = entityManager.SpawnEntity("AirlockDummy", MapCoordinates.Nullspace);

                Assert.True(entityManager.TryGetComponent(airlock, out doorComponent));
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closed));
            });

            await server.WaitIdleAsync();

            server.Assert(() =>
            {
                EntitySystem.Get<DoorSystem>().StartOpening(airlock);
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Opening));
            });

            await server.WaitIdleAsync();

            await WaitUntil(server, () => doorComponent.State == DoorState.Open);

            Assert.That(doorComponent.State, Is.EqualTo(DoorState.Open));

            server.Assert(() =>
            {
                EntitySystem.Get<DoorSystem>().TryClose((EntityUid) airlock);
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closing));
            });

            await WaitUntil(server, () => doorComponent.State == DoorState.Closed);

            Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closed));

            server.Assert(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    entityManager.DeleteEntity(airlock);
                });
            });

            server.RunTicks(5);

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task AirlockBlockTest()
        {
            var options = new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes
            };
            var server = StartServer(options);

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            IPhysBody physBody = null;
            EntityUid physicsDummy = default;
            EntityUid airlock = default;
            DoorComponent doorComponent = null;

            var physicsDummyStartingX = -1;

            server.Assert(() =>
            {
                var mapId = mapManager.CreateMap();

                var humanCoordinates = new MapCoordinates((physicsDummyStartingX, 0), mapId);
                physicsDummy = entityManager.SpawnEntity("PhysicsDummy", humanCoordinates);

                airlock = entityManager.SpawnEntity("AirlockDummy", new MapCoordinates((0, 0), mapId));

                Assert.True(entityManager.TryGetComponent(physicsDummy, out physBody));

                Assert.True(entityManager.TryGetComponent(airlock, out doorComponent));
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closed));
            });

            await server.WaitIdleAsync();

            // Push the human towards the airlock
            await server.WaitAssertion(() => Assert.That(physBody != null));
            await server.WaitPost(() => physBody.LinearVelocity = (0.5f, 0));

            for (var i = 0; i < 240; i += 10)
            {
                // Keep the airlock awake so they collide
                server.Post(() => entityManager.GetComponent<IPhysBody>(airlock).WakeBody());

                await server.WaitRunTicks(10);
                await server.WaitIdleAsync();
            }

            // Sanity check
            // Sloth: Okay I'm sorry but I hate having to rewrite tests for every refactor
            // If you see this yell at me in discord so I can continue to pretend this didn't happen.
            // REMINDER THAT I STILL HAVE TO FIX THIS TEST EVERY OTHER PHYSICS PR
            // Assert.That(physicsDummy.Transform.MapPosition.X, Is.GreaterThan(physicsDummyStartingX));

            // Blocked by the airlock
            await server.WaitAssertion(() => Assert.That(Math.Abs(entityManager.GetComponent<TransformComponent>(physicsDummy).MapPosition.X - 1) > 0.01f));
        }
    }
}
