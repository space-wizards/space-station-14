using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Handles movement for entities travelling through crawlable node networks.
/// </summary>
public sealed partial class NodeCrawlerMovementSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedNodeCrawlSystem _nodeCrawl = default!;

    [Dependency] private EntityQuery<MovementRelayTargetComponent> _movementRelayQuery;
    [Dependency] private EntityQuery<InputMoverComponent> _inputMoverQuery;
    [Dependency] private EntityQuery<CrawlableNodeComponent> _crawlableQuery;

    [SubscribeLocalEvent]
    private void OnMoveInput(Entity<NodeCrawlerMovementComponent> ent, ref MoveInputEvent args)
    {
        if (ent.Comp.Node is null)
            return;

        var moveVector = _mover.DirVecForButtons(args.Entity.Comp.HeldMoveButtons);
        if (ent.Comp.MoveVector == moveVector)
            return;

        ent.Comp.TargetNode = moveVector == Vector2.Zero
            ? GetDestination((ent, ent.Comp), ent.Comp.MoveVector)
            : null;
        ent.Comp.MoveVector = moveVector;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnBeforeMoverMove(Entity<NodeCrawlerMovementComponent> ent, ref BeforeMoverMoveEvent args)
    {
        if (ent.Comp.Node is null)
            return;

        var beforeMove = new NodeCrawlBeforeMoveEvent(ent, ent.Comp.MoveVector);
        RaiseLocalEvent(ent.Comp.Node!.Value, ref beforeMove);
        if (beforeMove.Handled)
        {
            StopMovement(ent);
            args.Handled = true;
            return;
        }

        if (ent.Comp.TargetNode is { } target)
            OngoingMovement(ent, target);
        else
            StartMovement(ent);

        args.Handled = ent.Comp.Node != null;
    }

    private void StartMovement(Entity<NodeCrawlerMovementComponent> mover)
    {
        if (GetDestination(mover, mover.Comp.MoveVector) is not { } target)
        {
            if (mover.Comp.Node is not { } node)
                return;

            if (!_crawlableQuery.TryGetComponent(node, out var nodeComp)
                || !nodeComp.DeadEnd)
                return;

            if (mover.Comp.HeldCrawler is not { } crawler)
                return;

            _nodeCrawl.ExitNodeCrawl(crawler);

            return;
        }

        mover.Comp.TargetNode = target;
        DirtyField(mover.AsNullable(), nameof(NodeCrawlerMovementComponent.TargetNode));

        OngoingMovement(mover, target);
    }

    private void StopMovement(Entity<NodeCrawlerMovementComponent> mover)
    {
        _physics.SetLinearVelocity(mover, Vector2.Zero);
        _physics.SetAngularVelocity(mover, 0);
    }

    private void OngoingMovement(Entity<NodeCrawlerMovementComponent> mover, EntityUid target)
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
            mover.Comp.TargetNode = null;
            DirtyField(mover, mover.Comp, nameof(NodeCrawlerMovementComponent.TargetNode));

            if (_movementRelayQuery.TryGetComponent(mover, out var movementTarget))
            {
                var ev = new NodeCrawlerArrivedAtNodeEvent(target, (mover.Owner, mover.Comp));
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

    private float MoveSpeed(EntityUid mover)
    {
        if (!_inputMoverQuery.TryGetComponent(mover, out var inputMover))
            return 0f;

        var moveSpeed = CompOrNull<MovementSpeedModifierComponent>(mover);

        var walkSpeed = moveSpeed?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
        var sprintSpeed = moveSpeed?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
        return inputMover.Sprinting ? sprintSpeed : walkSpeed;
    }

    private void PlayTraversalSound(Entity<NodeCrawlerMovementComponent> mover)
    {
        if (_gameTiming.CurTime < mover.Comp.NextTraversalSound)
            return;

        mover.Comp.NextTraversalSound = _gameTiming.CurTime + mover.Comp.TraversalSoundDelay;
        DirtyField(mover.AsNullable(), nameof(NodeCrawlerMovementComponent.NextTraversalSound));
        _audio.PlayPredicted(mover.Comp.TraversalSound, mover, mover);
    }

    private EntityUid? GetDestination(Entity<NodeCrawlerMovementComponent> ent, Vector2 moveVector)
    {
        if (moveVector == Vector2.Zero)
            return null;

        if (!_inputMoverQuery.TryGetComponent(ent, out var inputMover))
            return null;

        var target = _mover.GetParentGridAngle(inputMover).RotateVec(moveVector);
        if (ent.Comp.Node is not { } node || !Exists(node) || !_crawlableQuery.TryGetComponent(node, out var nodeCrawl))
            return null;

        var nodeXform = Transform(node);
        var nodeWorld = _transform.GetWorldPosition(nodeXform);
        var largestTarget = EntityUid.Invalid;
        var largestDot = 0.5d;

        foreach (var reachable in nodeCrawl.ReachableNodes)
        {
            if (!CanTraverseNode((ent, ent.Comp), node, reachable))
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
            if (_crawlableQuery.TryGetComponent(oldNode, out var oldNodeComp))
            {
                oldNodeComp.Crawlers.Remove(ent);
                DirtyField(oldNode, oldNodeComp, nameof(CrawlableNodeComponent.Crawlers));
            }
        }

        if (node is { } newNode)
        {
            var newNodeComp = Comp<CrawlableNodeComponent>(newNode);
            newNodeComp.Crawlers.Add(ent);
            DirtyField(newNode, newNodeComp, nameof(CrawlableNodeComponent.Crawlers));
        }

        ent.Comp.Node = node;
        DirtyField(ent.AsNullable(), nameof(NodeCrawlerMovementComponent.Node));
    }

    public void SetHeldCrawler(Entity<NodeCrawlerMovementComponent> ent, EntityUid? held)
    {
        if (ent.Comp.HeldCrawler == held)
            return;

        ent.Comp.HeldCrawler = held;
        DirtyField(ent.AsNullable(), nameof(NodeCrawlerMovementComponent.HeldCrawler));
    }
}
