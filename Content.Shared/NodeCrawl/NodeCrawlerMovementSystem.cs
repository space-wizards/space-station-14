using System.Numerics;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.NodeCrawl;

public sealed partial class NodeCrawlerMovementSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedNodeCrawlSystem _nodeCrawl = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NodeCrawlerMovementComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<NodeCrawlerMovementComponent, BeforeMoveEvent>(OnBeforeMoverMove);

        SubscribeLocalEvent<AtmosPipeLayersComponent, NodeCrawlCanTraverseEvent>(OnCanTraverse);
        SubscribeLocalEvent<AtmosPipeLayersComponent, NodeCrawlerArrivedAtNodeEvent>(OnArrived);
        SubscribeLocalEvent<GasPipeManifoldComponent, NodeCrawlBeforeMoveEvent>(OnBeforeMove);
    }

    private void OnMoveInput(Entity<NodeCrawlerMovementComponent> ent, ref MoveInputEvent args)
    {
        if (ent.Comp.Node is null)
            return;

        if (ent.Comp.MoveVector != args.MoveVec)
            ent.Comp.TargetNode = null;

        ent.Comp.MoveVector = args.MoveVec;
        Dirty(ent);
    }

    private void OnBeforeMoverMove(Entity<NodeCrawlerMovementComponent> ent, ref BeforeMoveEvent args)
    {
        if (ent.Comp.Node is null)
            return;

        if (!TryComp<InputMoverComponent>(ent, out var sharedMover))
            return;

        Entity<InputMoverComponent, NodeCrawlerMovementComponent> mover = (
            ent, sharedMover, ent.Comp);

        var beforeMove = new NodeCrawlBeforeMoveEvent((mover.Owner, mover.Comp2), mover.Comp2.MoveVector);
        RaiseLocalEvent(mover.Comp2.Node!.Value, ref beforeMove);
        if (beforeMove.Handled)
        {
            StopMovement(mover);
            args.Handled = true;
            return;
        }

        if (mover.Comp2.TargetNode is { } target)
            OngoingMovement(mover, target);
        else
            StartMovement(mover);

        args.Handled = ent.Comp.Node != null;
    }

    private void StartMovement(Entity<InputMoverComponent, NodeCrawlerMovementComponent> mover)
    {
        if (GetDestination(mover, mover.Comp2.MoveVector) is not { } target)
        {
            if (mover.Comp2.Node is not { } node)
                return;

            var nodeComp = Comp<CrawlableNodeComponent>(node);
            if (!nodeComp.DeadEnd)
                return;

            if (mover.Comp2.HeldCrawler is not { } crawler)
                return;

            _nodeCrawl.ExitNodeCrawl(crawler);

            return;
        }

        mover.Comp2.TargetNode = target;
        Dirty(mover, mover.Comp2);

        OngoingMovement(mover, target);
    }

    private void StopMovement(Entity<InputMoverComponent, NodeCrawlerMovementComponent> mover)
    {
        _physics.SetLinearVelocity(mover, Vector2.Zero);
        _physics.SetAngularVelocity(mover, 0);
    }

    private void OngoingMovement(Entity<InputMoverComponent, NodeCrawlerMovementComponent> mover, EntityUid target)
    {
        var speed = MoveSpeed(mover);

        var delta = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(mover);
        var frameMove = speed * (float)_gameTiming.FrameTime.TotalSeconds;

        // Snap to target if we would reach it this frame.
        if (delta.LengthSquared() <= frameMove * frameMove)
        {
            StopMovement(mover);
            _transform.SetWorldPosition(mover, _transform.GetWorldPosition(target));
            PlayTraversalSound(mover);
            SetNode((mover, mover), target);
            mover.Comp2.TargetNode = null;
            Dirty(mover, mover.Comp2);

            if (TryComp<MovementRelayTargetComponent>(mover, out var movementTarget))
            {
                var ev = new NodeCrawlerArrivedAtNodeEvent(target, (mover.Owner, mover.Comp2));
                RaiseLocalEvent(movementTarget.Source, ref ev);
            }

            StartMovement(mover);
            return;
        }

        var facing = Angle.FromWorldVec(delta);
        _transform.SetWorldRotation(mover, facing);

        var velocity = delta;
        velocity.Normalize();
        velocity *= speed;

        _physics.SetLinearVelocity(mover, velocity);
        _physics.SetAngularVelocity(mover, 0);
    }

    private float MoveSpeed(Entity<InputMoverComponent> mover)
    {
        var moveSpeed = CompOrNull<MovementSpeedModifierComponent>(mover);

        var walkSpeed = moveSpeed?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
        var sprintSpeed = moveSpeed?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
        return mover.Comp.Sprinting ? sprintSpeed : walkSpeed;
    }

    private void PlayTraversalSound(Entity<InputMoverComponent, NodeCrawlerMovementComponent> mover)
    {
        if (_gameTiming.CurTime <= mover.Comp2.LastTraversalSound + mover.Comp2.TraversalSoundDelay)
            return;

        mover.Comp2.LastTraversalSound = _gameTiming.CurTime;
        Dirty(mover, mover.Comp2);
        _audio.PlayPredicted(mover.Comp2.TraversalSound, mover, mover);
    }

    private EntityUid? GetDestination(Entity<InputMoverComponent, NodeCrawlerMovementComponent> ent, Vector2 moveVector)
    {
        if (moveVector == Vector2.Zero)
            return null;

        var target = _mover.GetParentGridAngle(ent.Comp1).RotateVec(moveVector);
        if (ent.Comp2.Node is not { } node || !Exists(node) || !TryComp<CrawlableNodeComponent>(node, out var nodeCrawl))
            return null;

        var nodeXform = Transform(node);
        var nodeWorld = _transform.GetWorldPosition(nodeXform);
        var largestTarget = EntityUid.Invalid;
        var largestDot = 0.5d;

        foreach (var reachable in nodeCrawl.ReachableNodes)
        {
            if (!CanTraverseNode((ent, ent.Comp2), node, reachable))
                continue;

            var reachableXform = Transform(reachable);
            var reachableWorld = _transform.GetWorldPosition(reachableXform);
            var delta = reachableWorld - nodeWorld;
            delta.Normalize();

            var deltaTargetDot = Vector2.Dot(delta, target);

            if (deltaTargetDot < largestDot)
                continue;

            largestTarget = reachable;
            largestDot = deltaTargetDot;
        }

        if (!largestTarget.Valid)
            return null;

        return largestTarget;
    }

    public bool CanTraverseNode(Entity<NodeCrawlerMovementComponent> mover, EntityUid from, EntityUid to)
    {
        var ev = new NodeCrawlCanTraverseEvent(mover, from, to);
        RaiseLocalEvent(to, ref ev);
        return !ev.Cancelled;
    }

    public void SetNode(Entity<NodeCrawlerMovementComponent> ent, EntityUid? node)
    {
        if (ent.Comp.Node == node)
            return;

        if (ent.Comp.Node is { } oldNode)
        {
            var oldNodeComp = Comp<CrawlableNodeComponent>(oldNode);
            oldNodeComp.Crawlers.Remove(ent);
            Dirty(oldNode, oldNodeComp);
        }

        if (node is { } newNode)
        {
            var newNodeComp = Comp<CrawlableNodeComponent>(newNode);
            newNodeComp.Crawlers.Add(ent);
            Dirty(newNode, newNodeComp);
        }

        ent.Comp.Node = node;
        Dirty(ent);
    }

    public void SetHeldCrawler(Entity<NodeCrawlerMovementComponent> ent, EntityUid? held)
    {
        if (ent.Comp.HeldCrawler == held)
            return;

        ent.Comp.HeldCrawler = held;
        Dirty(ent);
    }
}
