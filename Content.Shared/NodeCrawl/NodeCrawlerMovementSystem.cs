using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.NodeCrawl;

public sealed partial class NodeCrawlerMovementSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedNodeCrawlSystem _nodeCrawl = default!;

    public bool TryTick(Entity<InputMoverComponent, PhysicsComponent, TransformComponent> sharedMover)
    {
        if (!TryComp<NodeCrawlerMovementComponent>(sharedMover, out var crawler) || crawler.Node is null)
            return false;

        Entity<InputMoverComponent, PhysicsComponent, TransformComponent, NodeCrawlerMovementComponent> mover = (sharedMover.Owner, sharedMover.Comp1, sharedMover.Comp2, sharedMover.Comp3, crawler);

        if (mover.Comp4.TargetNode is { } target)
            OngoingMovement(mover, target);
        else
            StartMovement(mover);

        return true;
    }

    private void StartMovement(Entity<InputMoverComponent, PhysicsComponent, TransformComponent, NodeCrawlerMovementComponent> mover)
    {
        if (GetDestination(mover, mover.Comp1.HeldMoveButtons) is not { } target)
        {
            if (mover.Comp4.Node is not { } node)
                return;

            var nodeComp = Comp<CrawlableNodeComponent>(node);
            if (!nodeComp.DeadEnd)
                return;

            if (mover.Comp4.HeldCrawler is not { } crawler)
                return;

            _nodeCrawl.ExitNodeCrawl((crawler, Comp<NodeCrawlerComponent>(crawler)));
            return;
        }

        mover.Comp4.TargetNode = target;
        Dirty(mover, mover.Comp4);

        OngoingMovement(mover, target);
    }

    private void StopMovement(Entity<InputMoverComponent, PhysicsComponent, TransformComponent, NodeCrawlerMovementComponent> mover)
    {
        _physics.SetLinearVelocity(mover, Vector2.Zero, body: mover.Comp2);
        _physics.SetAngularVelocity(mover, 0, body: mover.Comp2);
    }

    private void OngoingMovement(Entity<InputMoverComponent, PhysicsComponent, TransformComponent, NodeCrawlerMovementComponent> mover, EntityUid target)
    {
        var speed = MoveSpeed(mover);

        var delta = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(mover.Comp3);
        if (delta.EqualsApprox(Vector2.Zero, speed * 0.01f))
        {
            StopMovement(mover);
            SetNode((mover, mover), target);
            mover.Comp4.TargetNode = null;
            Dirty(mover, mover.Comp4);

            if (TryComp<MovementRelayTargetComponent>(mover, out var movementTarget))
            {
                var evt = new NodeCrawlerArrivedAtNodeEvent(target);
                RaiseLocalEvent(movementTarget.Source, ref evt);
            }

            StartMovement(mover);
            return;
        }

        var facing = Angle.FromWorldVec(delta);
        _transform.SetWorldRotation(mover.Comp3, facing);

        var velocity = delta;
        velocity.Normalize();
        velocity *= speed;

        _physics.SetLinearVelocity(mover, velocity, body: mover.Comp2);
        _physics.SetAngularVelocity(mover, 0, body: mover.Comp2);
    }

    private float MoveSpeed(Entity<InputMoverComponent> mover)
    {
        var moveSpeed = CompOrNull<MovementSpeedModifierComponent>(mover);

        var walkSpeed = moveSpeed?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
        var sprintSpeed = moveSpeed?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
        return mover.Comp.Sprinting ? sprintSpeed : walkSpeed;
    }

    private EntityUid? GetDestination(Entity<InputMoverComponent, PhysicsComponent, TransformComponent, NodeCrawlerMovementComponent> ent, MoveButtons buttons)
    {
        if ((buttons & MoveButtons.AnyDirection) == 0)
            return null;

        var target = _mover.DirVecForButtons(buttons);
        target = _mover.GetParentGridAngle(ent.Comp1).RotateVec(target);
        if (ent.Comp4.Node is not { } node || !Exists(node) || !TryComp<CrawlableNodeComponent>(node, out var nodeCrawl))
            return null;

        var nodeXform = Transform(node);
        var nodeWorld = _transform.GetWorldPosition(nodeXform);
        var largestTarget = EntityUid.Invalid;
        var largestDot = 0d;

        foreach (var reachable in nodeCrawl.ReachableNodes)
        {
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

        if (!largestTarget.Valid || largestDot <= Math.Cos(ent.Comp4.RequiredAngle))
            return null;

        return largestTarget;
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
