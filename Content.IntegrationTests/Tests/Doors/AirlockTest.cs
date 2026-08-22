#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests.Doors;

[TestOf(typeof(AirlockComponent))]
public sealed class AirlockTest : GameTest
{
    private const string AirlockPhysicsDummy = "AirlockPhysicsDummy";
    private const string AirlockDummy = "AirlockDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {AirlockPhysicsDummy}
  id: {AirlockPhysicsDummy}
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
  name: {AirlockDummy}
  id: {AirlockDummy}
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

    [SidedDependency(Side.Server)] private DoorSystem _sDoorSystem = null!;
    [SidedDependency(Side.Server)] private SharedPhysicsSystem _sharedPhysics = null!;
    [SidedDependency(Side.Server)] private SharedTransformSystem _sXformSystem = null!;

    [Test]
    [Description("Check that door state transitions work correctly.")]
    public async Task OpenCloseDestroyTest()
    {
        Entity<DoorComponent> airlock = default;

        await Server.WaitAssertion(() =>
        {
            var uid = SSpawn(AirlockDummy);
            airlock = SEntity<DoorComponent>(uid);

            AssertDoorState(airlock, DoorState.Closed);
        });

        await RunUntilSynced();

        await Server.WaitAssertion(() =>
        {
            _sDoorSystem.StartOpening(airlock);
            AssertDoorState(airlock, DoorState.Opening);
        });

        await RunUntilSynced();

        await WaitForDoorState(airlock, DoorState.Open);

        await Server.WaitAssertion(() =>
        {
            _sDoorSystem.TryClose(airlock);
            AssertDoorState(airlock, DoorState.Closing);
        });

        await WaitForDoorState(airlock, DoorState.Closed);
    }

    [Test]
    [Description("Check that a closed airlock blocks the movement of a physics object.")]
    public async Task AirlockBlockTest()
    {
        PhysicsComponent? physBody = null;
        EntityUid airlockPhysicsDummy = default;
        Entity<DoorComponent> airlock = default;

        const int airlockPhysicsDummyStartingX = -1;

        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var dummyCoordinates = new EntityCoordinates(TestMap!.Grid, new Vector2(airlockPhysicsDummyStartingX, 0));
            airlockPhysicsDummy = SSpawnAtPosition(AirlockPhysicsDummy, dummyCoordinates);

            var uid = SSpawnAtPosition(AirlockDummy, TestMap.GridCoords);
            airlock = SEntity<DoorComponent>(uid);

            Assert.That(STryComp(airlockPhysicsDummy, out physBody), Is.True);
            AssertDoorState(airlock, DoorState.Closed);
        });

        await RunUntilSynced();

        // Push the dummy towards the airlock
        await Server.WaitAssertion(() => Assert.That(physBody, Is.Not.Null));
        await Server.WaitPost(() =>
        {
            _sharedPhysics.SetLinearVelocity(airlockPhysicsDummy, new Vector2(0.5f, 0f), body: physBody);
        });

        for (var i = 0; i < 240; i += 10)
        {
            // Keep the airlock awake so they collide
            await Server.WaitPost(() =>
            {
                _sharedPhysics.WakeBody(airlock);
            });
            AssertDoorState(airlock, DoorState.Closed);

            await RunTicksSync(10);
        }

        // Sanity check
        // Sloth: Okay I'm sorry but I hate having to rewrite tests for every refactor
        // If you see this yell at me in discord so I can continue to pretend this didn't happen.
        // REMINDER THAT I STILL HAVE TO FIX THIS TEST EVERY OTHER PHYSICS PR
        // _transform.GetMapCoordinates(UID HERE, xform: Assert.That(AirlockPhysicsDummy.Transform).X, Is.GreaterThan(AirlockPhysicsDummyStartingX));


        var airlockPhysicsDummyFinalX = _sXformSystem.GetWorldPosition(airlockPhysicsDummy).X;
        // The dummy moved, but was blocked by the airlock
        Assert.That(airlockPhysicsDummyFinalX, Is.GreaterThan(airlockPhysicsDummyStartingX).And.LessThanOrEqualTo(0));
    }

    /// <summary>
    /// Assert that the door is in a specific state.
    /// </summary>
    private static void AssertDoorState(Entity<DoorComponent> door, DoorState state)
    {
        Assert.That(door.Comp.State, Is.EqualTo(state));
    }

    /// <summary>
    /// Wait for the door to be in a specific state.
    /// </summary>
    private async Task WaitForDoorState(Entity<DoorComponent> door, DoorState state)
    {
        await PoolManager.WaitUntil(Server, () => door.Comp.State == state);
        AssertDoorState(door, state);
    }
}
