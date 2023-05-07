using System.Linq;
using Content.Server.Examine;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.NPC;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    private void ApplySeek(float[] interest, Vector2 direction, float weight)
    {
        if (weight == 0f || direction == Vector2.Zero)
            return;

        var directionAngle = (float) direction.ToAngle().Theta;

        for (var i = 0; i < InterestDirections; i++)
        {
            if (interest[i].Equals(-1f))
                continue;

            var angle = i * InterestRadians;
            var dot = MathF.Cos(directionAngle - angle);
            dot = (dot + 1) * 0.5f;
            interest[i] += dot * weight;
        }
    }

    #region Seek

    /// <summary>
    /// Attempts to head to the target destination, either via the next pathfinding node or the final target.
    /// </summary>
    private bool TrySeek(
        EntityUid uid,
        InputMoverComponent mover,
        NPCSteeringComponent steering,
        PhysicsComponent body,
        TransformComponent xform,
        Angle offsetRot,
        float moveSpeed,
        float[] interest,
        EntityQuery<PhysicsComponent> bodyQuery,
        float frameTime,
        ref bool forceSteer)
    {
        var ourCoordinates = xform.Coordinates;
        var destinationCoordinates = steering.Coordinates;

        // We've arrived, nothing else matters.
        if (xform.Coordinates.TryDistance(EntityManager, destinationCoordinates, out var distance) &&
            distance <= steering.Range)
        {
            steering.Status = SteeringStatus.InRange;
            return true;
        }

        // Grab the target position, either the next path node or our end goal..
        var targetCoordinates = GetTargetCoordinates(steering);
        var needsPath = false;

        // If the next node is invalid then get new ones
        if (!targetCoordinates.IsValid(EntityManager))
        {
            if (steering.CurrentPath.TryPeek(out var poly) &&
                (poly.Data.Flags & PathfindingBreadcrumbFlag.Invalid) != 0x0)
            {
                steering.CurrentPath.Dequeue();
                // Try to get the next node temporarily.
                targetCoordinates = GetTargetCoordinates(steering);
                needsPath = true;
                ResetStuck(steering, ourCoordinates);
            }
        }

        // Need to be pretty close if it's just a node to make sure LOS for door bashes or the likes.
        float arrivalDistance;

        if (targetCoordinates.Equals(steering.Coordinates))
        {
            // What's our tolerance for arrival.
            // If it's a pathfinding node it might be different to the destination.
            arrivalDistance = steering.Range;
        }
        // If next node is a free tile then get within its bounds.
        // This is to avoid popping it too early
        else if (steering.CurrentPath.TryPeek(out var node) && node.Data.IsFreeSpace)
        {
            arrivalDistance = MathF.Min(node.Box.Width / 2f, node.Box.Height / 2f) - 0.01f;
        }
        // Try getting into blocked range I guess?
        // TODO: Consider melee range or the likes.
        else
        {
            arrivalDistance = SharedInteractionSystem.InteractionRange - 0.05f;
        }

        // Check if mapids match.
        var targetMap = targetCoordinates.ToMap(EntityManager, _transform);
        var ourMap = ourCoordinates.ToMap(EntityManager, _transform);

        if (targetMap.MapId != ourMap.MapId)
        {
            steering.Status = SteeringStatus.NoPath;
            return false;
        }

        var direction = targetMap.Position - ourMap.Position;

        // Are we in range
        if (direction.Length <= arrivalDistance)
        {
            // Node needs some kind of special handling like access or smashing.
            if (steering.CurrentPath.TryPeek(out var node) && !node.Data.IsFreeSpace)
            {
                // Ignore stuck while handling obstacles.
                ResetStuck(steering, ourCoordinates);
                SteeringObstacleStatus status;

                // Breaking behaviours and the likes.
                lock (_obstacles)
                {
                    // We're still coming to a stop so wait for the do_after.
                    if (body.LinearVelocity.LengthSquared > 0.01f)
                    {
                        return true;
                    }

                    status = TryHandleFlags(uid, steering, node, bodyQuery);
                }

                // TODO: Need to handle re-pathing in case the target moves around.
                switch (status)
                {
                    case SteeringObstacleStatus.Completed:
                        steering.DoAfterId = null;
                        break;
                    case SteeringObstacleStatus.Failed:
                        steering.DoAfterId = null;
                        // TODO: Blacklist the poly for next query
                        steering.Status = SteeringStatus.NoPath;
                        return false;
                    case SteeringObstacleStatus.Continuing:
                        CheckPath(uid, steering, xform, needsPath, distance);
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Distance should already be handled above.
            // It was just a node, not the target, so grab the next destination (either the target or next node).
            if (steering.CurrentPath.Count > 0)
            {
                forceSteer = true;
                steering.CurrentPath.Dequeue();

                // Alright just adjust slightly and grab the next node so we don't stop moving for a tick.
                // TODO: If it's the last node just grab the target instead.
                targetCoordinates = GetTargetCoordinates(steering);
                targetMap = targetCoordinates.ToMap(EntityManager, _transform);

                // Can't make it again.
                if (ourMap.MapId != targetMap.MapId)
                {
                    SetDirection(mover, steering, Vector2.Zero);
                    steering.Status = SteeringStatus.NoPath;
                    return false;
                }

                // Gonna resume now business as usual
                direction = targetMap.Position - ourMap.Position;
                ResetStuck(steering, ourCoordinates);
            }
            else
            {
                // This probably shouldn't happen as we check above but eh.
                steering.Status = SteeringStatus.NoPath;
                return false;
            }
        }
        // Stuck detection
        // Check if we have moved further than the movespeed * stuck time.
        else if (AntiStuck &&
                 ourCoordinates.TryDistance(EntityManager, steering.LastStuckCoordinates, out var stuckDistance) &&
                 stuckDistance < NPCSteeringComponent.StuckDistance)
        {
            var stuckTime = _timing.CurTime - steering.LastStuckTime;
            // Either 1 second or how long it takes to move the stuck distance + buffer if we're REALLY slow.
            var maxStuckTime = Math.Max(1, NPCSteeringComponent.StuckDistance / moveSpeed * 1.2f);

            if (stuckTime.TotalSeconds > maxStuckTime)
            {
                // TODO: Blacklist nodes (pathfinder factor wehn)
                // TODO: This should be a warning but
                // A) NPCs get stuck on non-anchored static bodies still (e.g. closets)
                // B) NPCs still try to move in locked containers (e.g. cow, hamster)
                // and I don't want to spam grafana even harder than it gets spammed rn.
                _sawmill.Debug($"NPC {ToPrettyString(uid)} found stuck at {ourCoordinates}");
                steering.Status = SteeringStatus.NoPath;
                return false;
            }
        }
        else
        {
            ResetStuck(steering, ourCoordinates);
        }

        // Do we have no more nodes to follow OR has the target moved sufficiently? If so then re-path.
        if (!needsPath)
        {
            needsPath = steering.CurrentPath.Count == 0 || (steering.CurrentPath.Peek().Data.Flags & PathfindingBreadcrumbFlag.Invalid) != 0x0;
        }

        // TODO: Probably need partial planning support i.e. patch from the last node to where the target moved to.
        CheckPath(uid, steering, xform, needsPath, distance);

        // If we don't have a path yet then do nothing; this is to avoid stutter-stepping if it turns out there's no path
        // available but we assume there was.
        if (steering is { Pathfind: true, CurrentPath.Count: 0 })
            return true;

        if (moveSpeed == 0f || direction == Vector2.Zero)
        {
            steering.Status = SteeringStatus.NoPath;
            return false;
        }

        var input = direction.Normalized;
        var tickMovement = moveSpeed * frameTime;

        // We have the input in world terms but need to convert it back to what movercontroller is doing.
        input = offsetRot.RotateVec(input);
        var norm = input.Normalized;
        var weight = MapValue(direction.Length, tickMovement * 0.5f, tickMovement * 0.75f);

        ApplySeek(interest, norm, weight);

        // Prefer our current direction
        if (weight > 0f && body.LinearVelocity.LengthSquared > 0f)
        {
            const float sameDirectionWeight = 0.1f;
            norm = body.LinearVelocity.Normalized;

            ApplySeek(interest, norm, sameDirectionWeight);
        }

        return true;
    }

    private void ResetStuck(NPCSteeringComponent component, EntityCoordinates ourCoordinates)
    {
        component.LastStuckCoordinates = ourCoordinates;
        component.LastStuckTime = _timing.CurTime;
    }

    private void CheckPath(EntityUid uid, NPCSteeringComponent steering, TransformComponent xform, bool needsPath, float targetDistance)
    {
        if (!_pathfinding)
        {
            steering.CurrentPath.Clear();
            steering.PathfindToken?.Cancel();
            steering.PathfindToken = null;
            return;
        }

        if (!needsPath)
        {
            // If the target has sufficiently moved.
            var lastNode = GetCoordinates(steering.CurrentPath.Last());

            if (lastNode.TryDistance(EntityManager, steering.Coordinates, out var lastDistance) &&
                lastDistance > steering.RepathRange)
            {
                needsPath = true;
            }
        }

        // Request the new path.
        if (needsPath)
        {
            RequestPath(uid, steering, xform, targetDistance);
        }
    }

    /// <summary>
    /// We may be pathfinding and moving at the same time in which case early nodes may be out of date.
    /// </summary>
    public void PrunePath(EntityUid uid, MapCoordinates mapCoordinates, Vector2 direction, Queue<PathPoly> nodes)
    {
        if (nodes.Count <= 1)
            return;

        // Prune the first node as it's irrelevant (normally it is our node so we don't want to backtrack).
        nodes.Dequeue();
        // TODO: Really need layer support
        CollisionGroup mask = 0;

        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            mask = (CollisionGroup) physics.CollisionMask;
        }

        // If we have to backtrack (for example, we're behind a table and the target is on the other side)
        // Then don't consider pruning.
        var goal = nodes.Last().Coordinates.ToMap(EntityManager, _transform);
        var canPrune =
            _interaction.InRangeUnobstructed(mapCoordinates, goal, (goal.Position - mapCoordinates.Position).Length + 0.1f, mask);

        while (nodes.TryPeek(out var node))
        {
            if (!node.Data.IsFreeSpace)
                break;

            var nodeMap = node.Coordinates.ToMap(EntityManager, _transform);

            // If any nodes are 'behind us' relative to the target we'll prune them.
            // This isn't perfect but should fix most cases of stutter stepping.
            if (canPrune &&
                nodeMap.MapId == mapCoordinates.MapId &&
                Vector2.Dot(direction, nodeMap.Position - mapCoordinates.Position) < 0f)
            {
                nodes.Dequeue();
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Get the coordinates we should be heading towards.
    /// </summary>
    private EntityCoordinates GetTargetCoordinates(NPCSteeringComponent steering)
    {
        // Depending on what's going on we may return the target or a pathfind node.

        // Even if we're at the last node may not be able to head to target in case we get stuck on a corner or the likes.
        if (_pathfinding && steering.CurrentPath.Count >= 1 && steering.CurrentPath.TryPeek(out var nextTarget))
        {
            return GetCoordinates(nextTarget);
        }

        return steering.Coordinates;
    }

    /// <summary>
    /// Gets the fraction this value is between min and max
    /// </summary>
    /// <returns></returns>
    private float MapValue(float value, float minValue, float maxValue)
    {
        if (maxValue > minValue)
        {
            var mapped = (value - minValue) / (maxValue - minValue);
            return Math.Clamp(mapped, 0f, 1f);
        }

        return value >= minValue ? 1f : 0f;
    }

    #endregion

    #region Static Avoidance

    /// <summary>
    /// Tries to avoid static blockers such as walls.
    /// </summary>
    private void CollisionAvoidance(
        EntityUid uid,
        Angle offsetRot,
        Vector2 worldPos,
        float agentRadius,
        int layer,
        int mask,
        TransformComponent xform,
        float[] danger,
        List<Vector2> dangerPoints,
        EntityQuery<PhysicsComponent> bodyQuery,
        EntityQuery<TransformComponent> xformQuery)
    {
        var detectionRadius = MathF.Max(1f, agentRadius);

        foreach (var ent in _lookup.GetEntitiesInRange(uid, detectionRadius, LookupFlags.Static))
        {
            // TODO: If we can access the door or smth.
            if (ent == uid ||
                !bodyQuery.TryGetComponent(ent, out var otherBody) ||
                !otherBody.Hard ||
                !otherBody.CanCollide ||
                (mask & otherBody.CollisionLayer) == 0x0 &&
                (layer & otherBody.CollisionMask) == 0x0)
            {
                continue;
            }

            if (!_physics.TryGetNearestPoints(uid, ent, out var pointA, out var pointB, xform, xformQuery.GetComponent(ent)))
                continue;

            var obstacleDirection = pointB - pointA;
            var obstableDistance = obstacleDirection.Length;

            if (obstableDistance > detectionRadius)
                continue;

            // Fallback to worldpos if we're colliding.
            if (obstableDistance == 0f)
            {
                obstacleDirection = pointB - worldPos;
                obstableDistance = obstacleDirection.Length;

                if (obstableDistance == 0f)
                    continue;

                obstableDistance = agentRadius;
            }

            dangerPoints.Add(pointB);
            obstacleDirection = offsetRot.RotateVec(obstacleDirection);
            var norm = obstacleDirection.Normalized;
            var weight = obstableDistance <= agentRadius ? 1f : (detectionRadius - obstableDistance) / detectionRadius;

            for (var i = 0; i < InterestDirections; i++)
            {
                var dot = Vector2.Dot(norm, Directions[i]);
                danger[i] = MathF.Max(dot * weight * 0.9f, danger[i]);
            }
        }

    }

    #endregion

    #region Dynamic Avoidance

    /// <summary>
    /// Tries to avoid mobs of the same faction.
    /// </summary>
    private void Separation(
        EntityUid uid,
        Angle offsetRot,
        Vector2 worldPos,
        float agentRadius,
        int layer,
        int mask,
        PhysicsComponent body,
        TransformComponent xform,
        float[] danger,
        EntityQuery<PhysicsComponent> bodyQuery,
        EntityQuery<TransformComponent> xformQuery)
    {
        var detectionRadius = MathF.Max(0.35f, agentRadius + 0.1f);
        var ourVelocity = body.LinearVelocity;
        var factionQuery = GetEntityQuery<FactionComponent>();
        factionQuery.TryGetComponent(uid, out var ourFaction);

        foreach (var ent in _lookup.GetEntitiesInRange(uid, detectionRadius, LookupFlags.Dynamic))
        {
            // TODO: If we can access the door or smth.
            if (ent == uid ||
                !bodyQuery.TryGetComponent(ent, out var otherBody) ||
                !otherBody.Hard ||
                !otherBody.CanCollide ||
                (mask & otherBody.CollisionLayer) == 0x0 &&
                (layer & otherBody.CollisionMask) == 0x0 ||
                !factionQuery.TryGetComponent(ent, out var otherFaction) ||
                !_faction.IsFriendly(uid, ent, ourFaction, otherFaction) ||
                // Use <= 0 so we ignore stationary friends in case.
                Vector2.Dot(otherBody.LinearVelocity, ourVelocity) <= 0f)
            {
                continue;
            }

            var xformB = xformQuery.GetComponent(ent);

            if (!_physics.TryGetNearestPoints(uid, ent, out _, out var pointB, xform, xformB))
            {
                continue;
            }

            var obstacleDirection = pointB - worldPos;
            var obstableDistance = obstacleDirection.Length;

            if (obstableDistance > detectionRadius || obstableDistance == 0f)
                continue;

            obstacleDirection = offsetRot.RotateVec(obstacleDirection);
            var norm = obstacleDirection.Normalized;
            var weight = obstableDistance <= agentRadius ? 1f : (detectionRadius - obstableDistance) / detectionRadius;
            weight *= 1f;

            for (var i = 0; i < InterestDirections; i++)
            {
                var dot = Vector2.Dot(norm, Directions[i]);
                danger[i] = MathF.Max(dot * weight, danger[i]);
            }
        }
    }

    #endregion

    // TODO: Alignment

    // TODO: Cohesion
}
