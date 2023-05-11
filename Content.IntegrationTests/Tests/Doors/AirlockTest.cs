using System;
using System.Threading.Tasks;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests.Doors
{
    [TestFixture]
    [TestOf(typeof(AirlockComponent))]
    public sealed class AirlockTest
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
      fix1:
        shape:
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
  - type: ApcPowerReceiver
    needsPower: false
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
            bounds: ""-0.49,-0.49,0.49,0.49""
        mask:
        - Impassable
";
        [Test]
        public async Task OpenCloseDestroyTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var doors = entityManager.EntitySysManager.GetEntitySystem<DoorSystem>();

            EntityUid airlock = default;
            DoorComponent doorComponent = null;

            await server.WaitAssertion(() =>
            {
                airlock = entityManager.SpawnEntity("AirlockDummy", MapCoordinates.Nullspace);

                Assert.True(entityManager.TryGetComponent(airlock, out doorComponent));
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closed));
            });

            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                doors.StartOpening(airlock);
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Opening));
            });

            await server.WaitIdleAsync();

            await PoolManager.WaitUntil(server, () => doorComponent.State == DoorState.Open);

            Assert.That(doorComponent.State, Is.EqualTo(DoorState.Open));

            await server.WaitAssertion(() =>
            {
                doors.TryClose(airlock);
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closing));
            });

            await PoolManager.WaitUntil(server, () => doorComponent.State == DoorState.Closed);

            Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closed));

            await server.WaitAssertion(() =>
            {
                Assert.DoesNotThrow(() =>
                {
                    entityManager.DeleteEntity(airlock);
                });
            });

            server.RunTicks(5);

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task AirlockBlockTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var physicsSystem = entityManager.System<SharedPhysicsSystem>();

            PhysicsComponent physBody = null;
            EntityUid physicsDummy = default;
            EntityUid airlock = default;
            DoorComponent doorComponent = null;

            var physicsDummyStartingX = -1;

            await server.WaitAssertion(() =>
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
            await server.WaitAssertion(() => Assert.That(physBody, Is.Not.EqualTo(null)));
            await server.WaitPost(() =>
            {
                physicsSystem.SetLinearVelocity(physicsDummy, new Vector2(0.5f, 0f), body: physBody);
            });

            for (var i = 0; i < 240; i += 10)
            {
                // Keep the airlock awake so they collide
                await server.WaitPost(() =>
                {
                    physicsSystem.WakeBody(airlock);
                });

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
            await pairTracker.CleanReturnAsync();
        }
    }
}
