using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Timing;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Handles movement for entities travelling through crawlable node networks.
/// </summary>
public sealed partial class NodeCrawlerMovementSystem : VirtualController
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedNodeCrawlSystem _nodeCrawl = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<MovementRelayTargetComponent> _movementRelayQuery;
    [Dependency] private EntityQuery<InputMoverComponent> _inputMoverQuery;
    [Dependency] private EntityQuery<CrawlableNodeComponent> _crawlableQuery;
    [Dependency] private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        UpdatesBefore.Add(typeof(SharedMoverController));
        base.Initialize();
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<NodeCrawlerMovementComponent, InputMoverComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var movement, out _, out var xform))
        {
            if (movement.Node is null)
                continue;

            var beforeMove = new NodeCrawlBeforeMoveEvent((uid, movement), movement.MoveVector);
            RaiseLocalEvent(movement.Node.Value, ref beforeMove);
            if (beforeMove.Handled)
                continue;

            if (movement.MoveVector == Vector2.Zero)
                continue;

            if (movement.TargetNode is { } target)
                OngoingMovement((uid, movement), xform, target, frameTime);
            else
                StartMovement((uid, movement), xform, frameTime);
        }
    }

    [SubscribeLocalEvent]
    private void OnBeforeMoverMove(Entity<NodeCrawlerMovementComponent> ent, ref BeforeMoverMoveEvent args)
    {
        if (ent.Comp.Node is not null)
            args.Handled = true;
    }

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
        DirtyFields(ent.AsNullable(), null, nameof(NodeCrawlerMovementComponent.TargetNode), nameof(NodeCrawlerMovementComponent.MoveVector));
    }

    private void StartMovement(Entity<NodeCrawlerMovementComponent> mover, TransformComponent xform, float frameTime)
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

        OngoingMovement(mover, xform, target, frameTime);
    }

    private void OngoingMovement(Entity<NodeCrawlerMovementComponent> mover, TransformComponent xform, EntityUid target, float frameTime)
    {
        var speed = MoveSpeed(mover);
        var targetXform = _transformQuery.GetComponent(target);
        var delta = targetXform.LocalPosition - xform.LocalPosition;
        var frameMove = speed * frameTime;

        // Snap to target if we would reach it this physics step.
        if (delta.LengthSquared() <= frameMove * frameMove)
        {
            _transform.SetLocalPosition(mover, targetXform.LocalPosition, xform);
            PlayTraversalSound(mover);
            SetNode(mover, target);
            mover.Comp.TargetNode = null;
            DirtyField(mover.AsNullable(), nameof(NodeCrawlerMovementComponent.TargetNode));

            if (_movementRelayQuery.TryGetComponent(mover, out var movementTarget))
            {
                var ev = new NodeCrawlerArrivedAtNodeEvent(target, mover);
                RaiseLocalEvent(movementTarget.Source, ref ev);
            }

            return;
        }

        _transform.SetLocalRotation(mover, Angle.FromWorldVec(delta), xform);
        _transform.SetLocalPosition(mover, xform.LocalPosition + delta.Normalized() * frameMove, xform);
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

        var target = inputMover.RelativeRotation.RotateVec(moveVector);
        if (ent.Comp.Node is not { } node || !Exists(node) || !_crawlableQuery.TryGetComponent(node, out var nodeCrawl))
            return null;

        var nodeXform = _transformQuery.GetComponent(node);
        var nodePosition = nodeXform.LocalPosition;
        var largestTarget = EntityUid.Invalid;
        var largestDot = 0.5f;

        foreach (var reachable in nodeCrawl.ReachableNodes)
        {
            if (!CanTraverseNode((ent, ent.Comp), node, reachable))
                continue;

            var reachableXform = _transformQuery.GetComponent(reachable);
            var delta = reachableXform.LocalPosition - nodePosition;
            delta = delta.Normalized();

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
