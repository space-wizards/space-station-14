#nullable enable
using System.Numerics;
using Content.Shared.Friction;
using Content.Shared.Movement.Systems;
using Robust.Client.Timing;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Movement;

/// <summary>
/// This test attempts to check that lag compensation works.
/// However, this isn't a thorough test seeing as the test pairs don't actually have simulated latency.
/// </summary>
public sealed class LagCompensationTest : MovementTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: roomba
  components:
  - type: TestRoomba
  - type: Physics
    bodyType: KinematicController
    linearDamping: 0
  - type: LagCompensation
  - type: Fixtures
    fixtures: { fix1: { } }
";

    [Test]
    public async Task RoombaKickTest()
    {
        var pos = MapData.GridCoords.Offset(new Vector2(-1.5f, 0.5f));
        TargetCoords = SEntMan.GetNetCoordinates(Transform.WithEntityId(pos, MapData.MapUid));
        await SpawnTarget("roomba");

        // Initially roomba spawns adjacent to the player
        Assert.That(Delta(), Is.EqualTo(-2).Within(0.001));
        Assert.That(ClientDelta(), Is.EqualTo(-2).Within(0.001));

        // Roomba is stationary
        await RunTicks(5);
        Assert.That(Delta(), Is.EqualTo(-2).Within(0.001));
        Assert.That(ClientDelta(), Is.EqualTo(-2).Within(0.001));

        // Roomba is too far away to be kicked
        await Client.WaitPost(() => Client.System<RoombaController>().Kick(CTarget));
        var sComp = Comp<TestRoombaComponent>();
        var cComp = CEntMan.GetComponent<TestRoombaComponent>(CTarget.Value);
        Assert.That(cComp.Kicked, Is.Null);
        Assert.That(sComp.Kicked, Is.Null);
        await RunTicks(5);
        Assert.That(cComp.Kicked, Is.Null);
        Assert.That(sComp.Kicked, Is.Null);

        // Make the roomba start to rooomb
        sComp.Velocity.X = 1;
        var start = STiming.CurTick;

        // How far the roomba moves in one tick
        var tickDelta = sComp.Velocity.X * (float)STiming.TickPeriod.TotalSeconds;

        // The roomba will start to move on the server, but not on the client until it starts applying the relevant
        // server states
        var i = 0;
        while (CTiming.LastRealTick != start)
        {
            Assert.That(Delta(), Is.EqualTo(-2 + tickDelta * i).Within(0.001));
            Assert.That(ClientDelta(), Is.EqualTo(-2).Within(0.001));
            await RunTicks(1);
            i++;
        }

        // Next, we wait until the roomba is just about to go over one tile away on the client.
        while (ClientDelta() < 1 - tickDelta)
        {
            await RunTicks(1);
        }

        // The roomba is "in range" according to the client, but not according to the server
        Assert.That(Delta(), Is.GreaterThan(1 + tickDelta));
        Assert.That(ClientDelta(), Is.LessThan(1));

        // The client will attempt to kick the roomba, and the server should permit the boop, even though it is
        // technically out of range
        await Client.WaitPost(() => Client.System<RoombaController>().Kick(CTarget));
        Assert.That(cComp.Kicked, Is.EqualTo(CPlayer));
        Assert.That(sComp.Kicked, Is.Null);
        await RunTicks(4);
        Assert.That(cComp.Kicked, Is.EqualTo(CPlayer));
        Assert.That(sComp.Kicked, Is.EqualTo(SPlayer));

        // At this point, the kick will no longer be accepted.
        sComp.Kicked = null;
        Assert.That(Delta(), Is.GreaterThan(1 + tickDelta));
        Assert.That(ClientDelta(), Is.GreaterThan(1 + tickDelta));
        await Client.WaitPost(() => Client.System<RoombaController>().Kick(CTarget));
        Assert.That(sComp.Kicked, Is.Null);
        await RunTicks(5);
        Assert.That(sComp.Kicked, Is.Null);

    }
}

/// <summary>
/// Simple system & component that just makes some entity move around, and allows players to perform a basic lag
/// compensated interaction.
/// </summary>
public sealed partial class RoombaController : VirtualController
{
    [Dependency] private readonly SharedLagCompensationSystem _lag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(TileFrictionController)); // no friction please
        base.Initialize();
        SubscribeAllEvent<KickEvent>(OnKick);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<TestRoombaComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var roomba, out var physics))
        {
            PhysicsSystem.SetLinearVelocity(uid, roomba.Velocity, body: physics);
            PhysicsSystem.SetLinearDamping(uid, physics, 0);
        }
    }

    public void Kick(EntityUid? uid)
    {
        if (uid == null)
            return;

        var t = (IClientGameTiming)_timing;
        var ev = new KickEvent(GetNetEntity(uid.Value), t.LastRealTick);
        RaisePredictiveEvent(ev);
    }

    private void OnKick(KickEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (GetEntity(msg.Target) is not { Valid:true } target)
            return;

        var lagCoords = _lag.GetCoordinates(target, args.LastAppliedTick);

        var userPos = TransformSystem.GetWorldPosition(user);
        var targetPos = TransformSystem.ToWorldPosition(lagCoords);
        var delta = userPos - targetPos;
        if (delta.Length() > 1)
            return;

        var comp = Comp<TestRoombaComponent>(target);
        comp.Kicked = user;
        Dirty(target, comp);
    }

    [Serializable, NetSerializable]
    public sealed class KickEvent(NetEntity uid, GameTick tick) : EntityEventArgs
    {
        public NetEntity Target = uid;
        public GameTick Tick = tick;
    }
}

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class TestRoombaComponent : Component
{
    public Vector2 Velocity = Vector2.Zero;

    [AutoNetworkedField]
    public EntityUid? Kicked;
}
