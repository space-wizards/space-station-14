using Content.Server.Ghost.Components;
using Content.Shared.Administration;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed class TetherGunSystem : SharedTetherGunSystem
{
    [Dependency] private readonly IConGroupController _admin = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private readonly Dictionary<ICommonSession, (EntityUid Entity, EntityUid Tether, Joint Joint)> _tethered = new();
    private readonly HashSet<ICommonSession> _draggers = new();

    private const string JointId = "tether-joint";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<StartTetherEvent>(OnStartTether);
        SubscribeNetworkEvent<StopTetherEvent>(OnStopTether);
        SubscribeNetworkEvent<TetherMoveEvent>(OnMoveTether);

        _playerManager.PlayerStatusChanged += OnStatusChange;
    }

    private void OnStatusChange(object? sender, SessionStatusEventArgs e)
    {
        StopTether(e.Session);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnStatusChange;
    }

    public void Toggle(ICommonSession? session)
    {
        if (session == null)
            return;

        if (_draggers.Add(session))
        {
            RaiseNetworkEvent(new TetherGunToggleMessage()
            {
                Enabled = true,
            }, session.ConnectedClient);
            return;
        }

        _draggers.Remove(session);
        RaiseNetworkEvent(new TetherGunToggleMessage()
        {
            Enabled = false,
        }, session.ConnectedClient);
    }

    public bool IsEnabled(ICommonSession? session)
    {
        if (session == null)
            return false;

        return _draggers.Contains(session);
    }

    private void OnStartTether(StartTetherEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not IPlayerSession playerSession ||
            !_admin.CanCommand(playerSession, CommandName) ||
            !Exists(msg.Entity) ||
            Deleted(msg.Entity) ||
            msg.Coordinates == MapCoordinates.Nullspace ||
            _tethered.ContainsKey(args.SenderSession)) return;

        var tether = Spawn("TetherEntity", msg.Coordinates);

        if (!TryComp<PhysicsComponent>(tether, out var bodyA) ||
            !TryComp<PhysicsComponent>(msg.Entity, out var bodyB))
        {
            Del(tether);
            return;
        }

        EnsureComp<AdminFrozenComponent>(msg.Entity);

        if (TryComp<TransformComponent>(msg.Entity, out var xform))
        {
            xform.Anchored = false;
        }

        if (_container.IsEntityInContainer(msg.Entity))
        {
            xform?.AttachToGridOrMap();
        }

        if (TryComp<PhysicsComponent>(msg.Entity, out var body))
        {
            _physics.SetBodyStatus(body, BodyStatus.InAir);
        }

        _physics.WakeBody(tether, body: bodyA);
        _physics.WakeBody(msg.Entity, body: bodyB);
        var joint = _joints.CreateMouseJoint(tether, msg.Entity, id: JointId);

        SharedJointSystem.LinearStiffness(5f, 0.7f, bodyA.Mass, bodyB.Mass, out var stiffness, out var damping);
        joint.Stiffness = stiffness;
        joint.Damping = damping;
        joint.MaxForce = 10000f * bodyB.Mass;

        _tethered.Add(playerSession, (msg.Entity, tether, joint));
        RaiseNetworkEvent(new PredictTetherEvent()
        {
            Entity = msg.Entity
        }, args.SenderSession.ConnectedClient);
    }

    private void OnStopTether(StopTetherEvent msg, EntitySessionEventArgs args)
    {
        StopTether(args.SenderSession);
    }

    private void StopTether(ICommonSession session)
    {
        if (!_tethered.TryGetValue(session, out var weh))
            return;

        RemComp<AdminFrozenComponent>(weh.Entity);

        if (TryComp<PhysicsComponent>(weh.Entity, out var body) &&
            !HasComp<GhostComponent>(weh.Entity))
        {
            Timer.Spawn(1000, () =>
            {
                if (Deleted(weh.Entity)) return;

                _physics.SetBodyStatus(body, BodyStatus.OnGround);
            });
        }

        _joints.RemoveJoint(weh.Joint);
        Del(weh.Tether);
        _tethered.Remove(session);
    }

    private void OnMoveTether(TetherMoveEvent msg, EntitySessionEventArgs args)
    {
        if (!_tethered.TryGetValue(args.SenderSession, out var tether) ||
            !TryComp<TransformComponent>(tether.Tether, out var xform) ||
            xform.MapID != msg.Coordinates.MapId) return;

        xform.WorldPosition = msg.Coordinates.Position;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemove = new RemQueue<ICommonSession>();
        var bodyQuery = GetEntityQuery<PhysicsComponent>();

        foreach (var (session, entity) in _tethered)
        {
            if (Deleted(entity.Entity) ||
                Deleted(entity.Tether) ||
                !entity.Joint.Enabled)
            {
                toRemove.Add(session);
                continue;
            }

            // Force it awake, always
            if (bodyQuery.TryGetComponent(entity.Entity, out var body))
            {
                _physics.WakeBody(entity.Entity, body: body);
            }
        }

        foreach (var session in toRemove)
        {
            StopTether(session);
        }
    }
}
