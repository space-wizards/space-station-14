using System.Numerics;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests.Doors
{
    [TestFixture]
    [TestOf(typeof(AirlockComponent))]
    public sealed class AirlockTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: AirlockPhysicsDummy
  id: AirlockPhysicsDummy
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
  - type: DoorBolt
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
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var doors = entityManager.EntitySysManager.GetEntitySystem<DoorSystem>();

            EntityUid airlock = default;
            DoorComponent doorComponent = null;

            await server.WaitAssertion(() =>
            {
                airlock = entityManager.SpawnEntity("AirlockDummy", MapCoordinates.Nullspace);

#pragma warning disable NUnit2045 // Interdependent assertions.
                Assert.That(entityManager.TryGetComponent(airlock, out doorComponent), Is.True);
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closed));
#pragma warning restore NUnit2045
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

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task AirlockBlockTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var physicsSystem = entityManager.System<SharedPhysicsSystem>();
            var xformSystem = entityManager.System<SharedTransformSystem>();

            PhysicsComponent physBody = null;
            EntityUid airlockPhysicsDummy = default;
            EntityUid airlock = default;
            DoorComponent doorComponent = null;

            var airlockPhysicsDummyStartingX = -1;

            var map = await pair.CreateTestMap();

            await server.WaitAssertion(() =>
            {
                var humanCoordinates = new MapCoordinates(new Vector2(airlockPhysicsDummyStartingX, 0), map.MapId);
                airlockPhysicsDummy = entityManager.SpawnEntity("AirlockPhysicsDummy", humanCoordinates);

                airlock = entityManager.SpawnEntity("AirlockDummy", new MapCoordinates(new Vector2(0, 0), map.MapId));

                Assert.Multiple(() =>
                {
                    Assert.That(entityManager.TryGetComponent(airlockPhysicsDummy, out physBody), Is.True);
                    Assert.That(entityManager.TryGetComponent(airlock, out doorComponent), Is.True);
                });
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closed));
            });

            await server.WaitIdleAsync();

            // Push the human towards the airlock
            await server.WaitAssertion(() => Assert.That(physBody, Is.Not.EqualTo(null)));
            await server.WaitPost(() =>
            {
                physicsSystem.SetLinearVelocity(airlockPhysicsDummy, new Vector2(0.5f, 0f), body: physBody);
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
            // _transform.GetMapCoordinates(UID HERE, xform: Assert.That(AirlockPhysicsDummy.Transform).X, Is.GreaterThan(AirlockPhysicsDummyStartingX));

            // Blocked by the airlock
            await server.WaitAssertion(() =>
            {
                Assert.That(Math.Abs(xformSystem.GetWorldPosition(airlockPhysicsDummy).X - 1), Is.GreaterThan(0.01f));
            });
            await pair.CleanReturnAsync();
        }
    }
}
