using System.Collections.Generic;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
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
    public sealed class AirlockTest : GameTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: AirlockPhysicsDummy
  id: AirlockPhysicsDummy
  components:
  - type: Physics
    bodyType: Dynamic
  - type: GravityAffected
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

- type: entity
  name: DoorCollisionTestAirlock
  id: DoorCollisionTestAirlock
  components:
  - type: Door
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
            bounds: ""-0.49,-0.49,0.49,0.49""
        mask:
        - FullTileMask
        layer:
        - AirlockLayer

- type: entity
  name: DoorCollisionTestWindoor
  id: DoorCollisionTestWindoor
  components:
  - type: Door
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
            bounds: ""-0.49,-0.49,0.49,-0.36""
        layer:
        - BulletImpassable
        - WindoorImpassable

- type: entity
  name: DoorCollisionTestTable
  id: DoorCollisionTestTable
  components:
  - type: Transform
    anchored: true
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
            bounds: ""-0.45,-0.45,0.45,0.45""
        mask:
        - TableMask
        layer:
        - TableLayer

- type: entity
  name: DoorCollisionTestConveyor
  id: DoorCollisionTestConveyor
  components:
  - type: Transform
    anchored: true
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      conveyor:
        shape:
          !type:PhysShapeAabb
            bounds: ""-0.50,-0.50,0.50,0.50""
        layer:
        - ConveyorMask
        hard: false

- type: entity
  name: DoorCollisionTestWall
  id: DoorCollisionTestWall
  components:
  - type: Transform
    anchored: true
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
            bounds: ""-0.5,-0.5,0.5,0.5""
        mask:
        - FullTileMask
        layer:
        - WallLayer

- type: entity
  name: DoorCollisionTestShutter
  id: DoorCollisionTestShutter
  components:
  - type: Door
  - type: Transform
    anchored: true
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
            bounds: ""-0.5,-0.5,0.5,0.5""
        mask:
        - FullTileMask
        layer:
        - AirlockLayer

- type: entity
  name: DoorCollisionTestMob
  id: DoorCollisionTestMob
  components:
  - type: Physics
    bodyType: Dynamic
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeCircle
            radius: 0.35
        mask:
        - MobMask
        layer:
        - MobLayer

- type: entity
  name: DoorCollisionTestMouse
  id: DoorCollisionTestMouse
  components:
  - type: Physics
    bodyType: Dynamic
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeCircle
            radius: 0.2
        mask:
        - SmallMobMask
        layer:
        - SmallMobLayer
";
        [Test]
        public async Task OpenCloseDestroyTest()
        {
            var pair = Pair;
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
        }

        [Test]
        public async Task AirlockBlockTest()
        {
            var pair = Pair;
            var server = pair.Server;

            await server.WaitIdleAsync();

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
        }

        [Test]
        public async Task WindoorCanCloseOverTable()
        {
            var (_, doors, door, _) = await SpawnDoorCollisionScenario(
                "DoorCollisionTestWindoor",
                ("DoorCollisionTestTable", Vector2.Zero));

            await Pair.Server.WaitAssertion(() =>
            {
                Assert.That(doors.CanClose(door), Is.True);
            });
        }

        [Test]
        public async Task WindoorCanCloseOverShutter()
        {
            var (_, doors, door, _) = await SpawnDoorCollisionScenario(
                "DoorCollisionTestWindoor",
                ("DoorCollisionTestShutter", Vector2.Zero));

            await Pair.Server.WaitAssertion(() =>
            {
                Assert.That(doors.CanClose(door), Is.True);
            });
        }

        [Test]
        public async Task WindoorCannotCloseOverMob()
        {
            var (_, doors, door, _) = await SpawnDoorCollisionScenario(
                "DoorCollisionTestWindoor",
                ("DoorCollisionTestMob", new Vector2(0, -0.2f)));

            await Pair.Server.WaitAssertion(() =>
            {
                Assert.That(doors.CanClose(door), Is.False);
            });
        }

        [Test]
        public async Task ShutterStillCollidesWithMob()
        {
            var (_, doors, door, obstacles) = await SpawnDoorCollisionScenario(
                "DoorCollisionTestShutter",
                ("DoorCollisionTestMob", Vector2.Zero));

            var mob = obstacles[0];

            await Pair.Server.WaitAssertion(() =>
            {
                var colliding = new HashSet<EntityUid>();
                doors.GetColliding(door, colliding);

                Assert.That(colliding, Does.Contain(mob));
            });
        }

        [Test]
        public async Task AirlockCanCloseOverConveyor()
        {
            var (_, doors, door, _) = await SpawnDoorCollisionScenario(
                "DoorCollisionTestAirlock",
                ("DoorCollisionTestConveyor", Vector2.Zero));

            await Pair.Server.WaitAssertion(() =>
            {
                Assert.That(doors.CanClose(door), Is.True);
            });
        }

        [Test]
        public async Task NeighboringFullTileWallsDoNotBlockDoor()
        {
            var (_, doors, door, _) = await SpawnDoorCollisionScenario(
                "DoorCollisionTestAirlock",
                ("DoorCollisionTestWall", new Vector2(1, 0)),
                ("DoorCollisionTestWall", new Vector2(-1, 0)),
                ("DoorCollisionTestWall", new Vector2(0, 1)),
                ("DoorCollisionTestWall", new Vector2(0, -1)));

            await Pair.Server.WaitAssertion(() =>
            {
                Assert.That(doors.CanClose(door), Is.True);
            });
        }

        [Test]
        public async Task GetCollidingReturnsBlockingMobButNotMouse()
        {
            var (_, doors, door, obstacles) = await SpawnDoorCollisionScenario(
                "DoorCollisionTestAirlock",
                ("DoorCollisionTestMob", Vector2.Zero),
                ("DoorCollisionTestMouse", Vector2.Zero));

            var mob = obstacles[0];
            var mouse = obstacles[1];

            await Pair.Server.WaitAssertion(() =>
            {
                var colliding = new HashSet<EntityUid>();
                doors.GetColliding(door, colliding);

                Assert.Multiple(() =>
                {
                    Assert.That(colliding, Does.Contain(mob));
                    Assert.That(colliding, Does.Not.Contain(mouse));
                    Assert.That(doors.CanClose(door), Is.False);
                });
            });
        }

        [Test]
        public async Task BlockedPartialCloseReversesThroughOpeningBeforeRetrying()
        {
            var (entityManager, doors, door, _) = await SpawnDoorCollisionScenario("DoorCollisionTestAirlock");
            var server = Pair.Server;
            EntityUid mob = default;
            DoorComponent doorComponent = default!;
            bool partialCloseResult = true;

            await server.WaitPost(() =>
            {
                Assert.That(entityManager.TryGetComponent(door, out doorComponent), Is.True);
                Assert.That(doors.TryClose(door), Is.True);
                Assert.That(doorComponent.State, Is.EqualTo(DoorState.Closing));

                var coordinates = entityManager.GetComponent<TransformComponent>(door).Coordinates;
                mob = entityManager.SpawnEntity("DoorCollisionTestMob", coordinates);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            await server.WaitPost(() =>
            {
                partialCloseResult = doors.OnPartialClose(door);
            });

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(partialCloseResult, Is.False);
                    Assert.That(doorComponent.State, Is.EqualTo(DoorState.Opening));
                    Assert.That(doorComponent.Partial, Is.True);
                    Assert.That(doorComponent.NextStateChange, Is.Not.Null);
                });
            });

            await PoolManager.WaitUntil(server, () => doorComponent.State == DoorState.Open);

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(doorComponent.State, Is.EqualTo(DoorState.Open));
                    Assert.That(doorComponent.Partial, Is.False);
                    Assert.That(doorComponent.NextStateChange, Is.Not.Null);
                    Assert.That(entityManager.Deleted(mob), Is.False);
                });
            });
        }

        private async Task<(IEntityManager EntityManager, DoorSystem Doors, EntityUid Door, List<EntityUid> Obstacles)> SpawnDoorCollisionScenario(
            string doorPrototype,
            params (string Prototype, Vector2 Position)[] obstacles)
        {
            var pair = Pair;
            var server = pair.Server;
            var map = await pair.CreateTestMap();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var doors = entityManager.System<DoorSystem>();
            var mapSystem = entityManager.System<SharedMapSystem>();
            var obstacleEntities = new List<EntityUid>();
            EntityUid door = default;

            await server.WaitPost(() =>
            {
                mapSystem.SetTile(map.Grid, new Vector2i(1, 0), map.Tile.Tile);
                door = entityManager.SpawnEntity(doorPrototype, new EntityCoordinates(map.Grid.Owner, Vector2.Zero));

                foreach (var obstacle in obstacles)
                {
                    obstacleEntities.Add(entityManager.SpawnEntity(
                        obstacle.Prototype,
                        new EntityCoordinates(map.Grid.Owner, obstacle.Position)));
                }

                doors.SetState(door, DoorState.Open);
            });

            await server.WaitRunTicks(1);
            await server.WaitIdleAsync();

            return (entityManager, doors, door, obstacleEntities);
        }
    }
}
