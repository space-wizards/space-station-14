using System.Linq;
using System.Numerics;
using Content.Server.Examine;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Climbing;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.NPC;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using ClimbingComponent = Content.Shared.Climbing.Components.ClimbingComponent;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    private void ApplySeek(Span<float> interest, Vector2 direction, float weight)
    {
        if (weight == 0f || direction == Vector2.Zero)
            return;

        var directionAngle = (float)direction.ToAngle().Theta;

        for (var i = 0; i < InterestDirections; i++)
        {
            var angle = i * InterestRadians;
            var dot = MathF.Cos(directionAngle - angle);
            dot = (dot + 1f) * 0.5f;
            interest[i] = Math.Clamp(interest[i] + dot * weight, 0f, 1f);
        }
    }

    #region Seek

    /// <summary>
    /// Takes into account agent-specific context that may allow it to bypass a node which is not FreeSpace.
    /// </summary>
    private bool IsFreeSpace(
        EntityUid uid,
        NPCSteeringComponent steering,
        PathPoly node)
    {
        if (node.Data.IsFreeSpace)
        {
            return true;
        }
        // Handle the case where the node is a climb, we can climb, and we are climbing.
        else if ((node.Data.Flags & PathfindingBreadcrumbFlag.Climb) != 0x0 &&
            (steering.Flags & PathFlags.Climbing) != 0x0 &&
            TryComp<ClimbingComponent>(uid, out var climbing) &&
            climbing.IsClimbing)
        {
            return true;
        }

        // TODO: Ideally for "FreeSpace" we check all entities on the tile and build flags dynamically (pathfinder refactor in future).
        var ents = _entSetPool.Get();
        _lookup.GetLocalEntitiesIntersecting(node.GraphUid, node.Box.Enlarged(-0.04f), ents, flags: LookupFlags.Static);
        var result = true;

        if (ents.Count > 0)
        {
            var fixtures = _fixturesQuery.GetComponent(uid);
            var physics = _physicsQuery.GetComponent(uid);

            foreach (var intersecting in ents)
            {
                if (!_physics.IsCurrentlyHardCollidable((uid, fixtures, physics), intersecting))
                {
                    continue;
                }

                result = false;
                break;
            }
        }

        _entSetPool.Return(ents);
        return result;
    }

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
        float acceleration,
        float friction,
        Span<float> interest,
        float frameTime,
        ref bool forceSteer,
        ref float moveMultiplier)
    {
        var ourCoordinates = xform.Coordinates;
        var destinationCoordinates = steering.Coordinates;
        var inLos = true;

        // check if we should ignore all pathing logic and go straight to the target coordinates
        var directMove = steering.DirectMove;

        // Check if we're in LOS if that's required.
        // TODO: Need something uhh better not sure on the interaction between these.
        if (!steering.ForceMove && steering.ArriveOnLineOfSight)
        {
            // TODO: use vision range
            inLos = _interaction.InRangeUnobstructed(uid, steering.Coordinates, 10f);

            if (inLos)
            {
                steering.LineOfSightTimer += frameTime;

                if (steering.LineOfSightTimer >= steering.LineOfSightTimeRequired)
                {
                    steering.Status = SteeringStatus.InRange;
                    ResetStuck(steering, ourCoordinates);
                    return true;
                }
            }
            else
            {
                steering.LineOfSightTimer = 0f;
            }
        }
        else
        {
            steering.LineOfSightTimer = 0f;
            steering.ForceMove = false;
        }

        var velLen = body.LinearVelocity.Length();

        var careAboutSpeed = steering.InRangeMaxSpeed != null;
        var finalInRange = ourCoordinates.TryDistance(EntityManager, destinationCoordinates, out var targetDistance) && inLos && targetDistance <= steering.Range;
        var velocityHigh = careAboutSpeed && velLen > steering.InRangeMaxSpeed!.Value;
        // if we're in range and we care about velocity, stop trying to move if we early return
        if (finalInRange && careAboutSpeed)
            moveMultiplier = 0f;

        // We've arrived and velocity is acceptable, nothing else matters.
        if (finalInRange && !velocityHigh)
        {
            steering.Status = SteeringStatus.InRange;
            ResetStuck(steering, ourCoordinates);
            return true;
        }

        // Grab the target position, either the next path node or our end goal..
        var targetCoordinates = steering.DirectMove ? steering.Coordinates : GetTargetCoordinates(steering);

        if (!targetCoordinates.IsValid(EntityManager))
        {
            steering.Status = SteeringStatus.NoPath;
            return false;
        }

        var needsPath = false;

        // If the next node is invalid then get new ones
        if (!targetCoordinates.IsValid(EntityManager))
        {
            if (!directMove && steering.CurrentPath.TryPeek(out var poly) &&
                (poly.Data.Flags & PathfindingBreadcrumbFlag.Invalid) != 0x0)
            {
                steering.CurrentPath.Dequeue();
                // Try to get the next node temporarily.
                targetCoordinates = GetTargetCoordinates(steering);
                needsPath = true;
                ResetStuck(steering, ourCoordinates);
            }
        }

        // Check if mapids match.
        var targetMap = _transform.ToMapCoordinates(targetCoordinates);
        var ourMap = _transform.ToMapCoordinates(ourCoordinates);

        if (targetMap.MapId != ourMap.MapId)
        {
            steering.Status = SteeringStatus.NoPath;
            return false;
        }

        var direction = targetMap.Position - ourMap.Position;

        // Need to be pretty close if it's just a node to make sure LOS for door bashes or the likes.
        bool arrived;

        if (targetCoordinates.Equals(steering.Coordinates))
        {
            // What's our tolerance for arrival.
            // If it's a pathfinding node it might be different to the destination.
            arrived = direction.Length() <= steering.Range;
        }
        // If next node is a free tile then get within its bounds.
        // This is to avoid popping it too early
        else if (steering.CurrentPath.TryPeek(out var node) && IsFreeSpace(uid, steering, node))
        {
            arrived = node.Box.Contains(ourCoordinates.Position);
        }
        // Try getting into blocked range I guess?
        // TODO: Consider melee range or the likes.
        else
        {
            arrived = direction.Length() <= SharedInteractionSystem.InteractionRange - 0.05f;
        }

        // Are we in range
        if (arrived)
        {
            // Node needs some kind of special handling like access or smashing.
            if (!directMove && steering.CurrentPath.TryPeek(out var node) && !IsFreeSpace(uid, steering, node))
            {
                // Ignore stuck while handling obstacles.
                ResetStuck(steering, ourCoordinates);
                SteeringObstacleStatus status;

                // Breaking behaviours and the likes.
                lock (_obstacles)
                {
                    // We're still coming to a stop so wait for the do_after.
                    if (body.LinearVelocity.LengthSquared() > 0.01f)
                    {
                        return true;
                    }

                    status = TryHandleFlags(uid, steering, node);
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
                        CheckPath(uid, steering, xform, needsPath, targetDistance);
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Distance should already be handled above.
            // It was just a node, not the target, so grab the next destination (either the target or next node).
            if (!directMove && steering.CurrentPath.Count > 0)
            {
                forceSteer = true;
                steering.CurrentPath.Dequeue();

                // Alright just adjust slightly and grab the next node so we don't stop moving for a tick.
                // TODO: If it's the last node just grab the target instead.
                targetCoordinates = GetTargetCoordinates(steering);

                if (!targetCoordinates.IsValid(EntityManager))
                {
                    SetDirection(uid, mover, steering, Vector2.Zero);
                    steering.Status = SteeringStatus.NoPath;
                    return false;
                }

                targetMap = _transform.ToMapCoordinates(targetCoordinates);

                // Can't make it again.
                if (ourMap.MapId != targetMap.MapId)
                {
                    SetDirection(uid, mover, steering, Vector2.Zero);
                    steering.Status = SteeringStatus.NoPath;
                    return false;
                }

                // Gonna resume now business as usual
                direction = targetMap.Position - ourMap.Position;
                ResetStuck(steering, ourCoordinates);
            }
            else
            {
                needsPath = true;
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
                Log.Debug($"NPC {ToPrettyString(uid)} found stuck at {ourCoordinates}");
                needsPath = true;

                if (stuckTime.TotalSeconds > maxStuckTime * 3)
                {
                    steering.Status = SteeringStatus.NoPath;
                    return false;
                }
            }
        }
        else
        {
            ResetStuck(steering, ourCoordinates);
        }

        // If not in LOS and no path then get a new one fam.
        if (!directMove &&
            ((!inLos && steering.ArriveOnLineOfSight && steering.CurrentPath.Count == 0) ||
             (!steering.ArriveOnLineOfSight && steering.CurrentPath.Count == 0)))
        {
            needsPath = true;
        }

        // TODO: Probably need partial planning support i.e. patch from the last node to where the target moved to.
        if (!directMove)
            CheckPath(uid, steering, xform, needsPath, targetDistance);

        // whether we should want to brake right now
        var haveToBrake = finalInRange && velocityHigh;

        // If we don't have a path yet then do nothing; this is to avoid stutter-stepping if it turns out there's no path
        // available but we assume there was. Brake if we have to, though.
        if (!directMove && steering is { Pathfind: true, CurrentPath.Count: 0 } && !haveToBrake)
            return true;

        if (moveSpeed == 0f || direction == Vector2.Zero)
        {
            steering.Status = SteeringStatus.NoPath;
            return false;
        }

        var moveType = MovementType.MovingToTarget;

        var realAccel = acceleration * moveSpeed;
        var frameAccel = realAccel * frameTime;

        // check our tangential velocity
        var normVel = direction * Vector2.Dot(body.LinearVelocity, direction) / direction.LengthSquared();
        var tgVel = body.LinearVelocity - normVel;

        // we're near final node but haven't braked, do so
        if (haveToBrake)
        {
            // how much distance we'll pass before hitting our desired max speed
            var brakePath = (velLen - steering.InRangeMaxSpeed ?? 0f) / friction;
            var hardBrake = brakePath > MathF.Min(0.5f, steering.Range); // hard brake if it takes more than half a tile

            moveType = hardBrake ? MovementType.Braking : MovementType.Coasting;
        }
        else
        {
            // scary magic number but shouldn't be a datafield since what this actually does is implementation-dependent
            const float circlingTolerance = 0.5f;

            var dirLen = direction.Length();
            // tangentially brake if we'll be spiraling outwards at our current tangential velocity
            var tangentialBrake = !arrived && realAccel * circlingTolerance < tgVel.LengthSquared() / dirLen;

            moveType = tangentialBrake ? MovementType.BrakingTangential : MovementType.MovingToTarget;
        }

        switch (moveType)
        {
            case MovementType.MovingToTarget:
                moveMultiplier = 1f;
                ApplySeek(interest, offsetRot.RotateVec(direction.Normalized()), 1f);
                break;
            case MovementType.Braking:
                if (velLen > 0f)
                {
                    // copy our velocity and apply friction to the copy
                    var cvel = body.LinearVelocity;
                    _mover.Friction(0f, frameTime, friction, ref cvel);
                    // clamp our braking to what our post-friction velocity would be
                    // otherwise we can overbrake in this frame and reverse movement direction
                    // TODO: a way to tell calling code that we don't want to reverse movement direction to not have to do this
                    moveMultiplier = MapValue(cvel.Length(), 0f, frameAccel);
                                        // brake                                 // normalise
                    ApplySeek(interest, -offsetRot.RotateVec(body.LinearVelocity / velLen), 1f);
                }
                break;
            case MovementType.BrakingTangential:
                if (velLen > 0f)
                {
                    moveMultiplier = MapValue(tgVel.Length(), 0f, frameAccel);
                                        // brake
                    ApplySeek(interest, -offsetRot.RotateVec(tgVel.Normalized()), tgVel.Length() / velLen);
                }
                break;
            case MovementType.Coasting:
                moveMultiplier = 0f;
                break;
        }

        return true;
    }

    // used in TrySeek()
    private enum MovementType
    {
        MovingToTarget,
        Braking,
        BrakingTangential,
        Coasting
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

        if (!needsPath && steering.CurrentPath.Count > 0)
        {
            needsPath = steering.CurrentPath.Count > 0 && (steering.CurrentPath.Peek().Data.Flags & PathfindingBreadcrumbFlag.Invalid) != 0x0;

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
    public void PrunePath(EntityUid uid, MapCoordinates mapCoordinates, Vector2 direction, List<PathPoly> nodes)
    {
        if (nodes.Count <= 1)
            return;

        // Work out if we're inside any nodes, then use the next one as the starting point.
        var index = 0;
        var found = false;

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var matrix = _transform.GetWorldMatrix(node.GraphUid);

            // Always want to prune the poly itself so we point to the next poly and don't backtrack.
            if (matrix.TransformBox(node.Box).Contains(mapCoordinates.Position))
            {
                index = i + 1;
                found = true;
                break;
            }
        }

        if (found)
        {
            nodes.RemoveRange(0, index);
            _pathfindingSystem.Simplify(nodes);
            return;
        }

        // Otherwise, take the node after the nearest node.

        // TODO: Really need layer support
        CollisionGroup mask = 0;

        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            mask = (CollisionGroup)physics.CollisionMask;
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];

            if (!node.Data.IsFreeSpace)
                break;

            var nodeMap = _transform.ToMapCoordinates(node.Coordinates);

            // If any nodes are 'behind us' relative to the target we'll prune them.
            // This isn't perfect but should fix most cases of stutter stepping.
            if (nodeMap.MapId == mapCoordinates.MapId &&
                Vector2.Dot(direction, nodeMap.Position - mapCoordinates.Position) < 0f)
            {
                nodes.RemoveAt(i);
                continue;
            }

            break;
        }

        _pathfindingSystem.Simplify(nodes);
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
        Span<float> danger)
    {
        var objectRadius = 0.25f;
        var detectionRadius = MathF.Max(0.35f, agentRadius + objectRadius);
        var ents = _entSetPool.Get();
        _lookup.GetEntitiesInRange(uid, detectionRadius, ents, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate);

        foreach (var ent in ents)
        {
            // TODO: If we can access the door or smth.
            if (!_physicsQuery.TryGetComponent(ent, out var otherBody) ||
                !otherBody.Hard ||
                !otherBody.CanCollide ||
                otherBody.BodyType == BodyType.KinematicController ||
                (mask & otherBody.CollisionLayer) == 0x0 &&
                (layer & otherBody.CollisionMask) == 0x0)
            {
                continue;
            }

            var xformB = _xformQuery.GetComponent(ent);

            if (!_physics.TryGetNearest(uid, ent,
                    out var pointA, out var pointB, out var distance,
                    xform, xformB))
            {
                continue;
            }

            if (distance > detectionRadius)
                continue;

            var weight = 1f;
            var obstacleDirection = pointB - pointA;

            // Inside each other so just use worldPos
            if (distance == 0f)
            {
                obstacleDirection = _transform.GetWorldPosition(xformB) - worldPos;
            }
            else
            {
                weight = (detectionRadius - distance) / detectionRadius;
            }

            if (obstacleDirection == Vector2.Zero)
                continue;

            obstacleDirection = offsetRot.RotateVec(obstacleDirection);
            var norm = obstacleDirection.Normalized();

            for (var i = 0; i < InterestDirections; i++)
            {
                var dot = Vector2.Dot(norm, Directions[i]);
                danger[i] = MathF.Max(dot * weight, danger[i]);
            }
        }

        _entSetPool.Return(ents);
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
        Span<float> danger)
    {
        var objectRadius = 0.25f;
        var detectionRadius = MathF.Max(0.35f, agentRadius + objectRadius);
        var ourVelocity = body.LinearVelocity;
        _factionQuery.TryGetComponent(uid, out var ourFaction);
        var ents = _entSetPool.Get();
        _lookup.GetEntitiesInRange(uid, detectionRadius, ents, LookupFlags.Dynamic | LookupFlags.Approximate);

        foreach (var ent in ents)
        {
            // TODO: If we can access the door or smth.
            if (!_physicsQuery.TryGetComponent(ent, out var otherBody) ||
                !otherBody.Hard ||
                !otherBody.CanCollide ||
                (mask & otherBody.CollisionLayer) == 0x0 &&
                (layer & otherBody.CollisionMask) == 0x0 ||
                !_factionQuery.TryGetComponent(ent, out var otherFaction) ||
                !_npcFaction.IsEntityFriendly((uid, ourFaction), (ent, otherFaction)) ||
                // Use <= 0 so we ignore stationary friends in case.
                Vector2.Dot(otherBody.LinearVelocity, ourVelocity) <= 0f)
            {
                continue;
            }

            var xformB = _xformQuery.GetComponent(ent);

            if (!_physics.TryGetNearest(uid, ent, out var pointA, out var pointB, out var distance, xform, xformB))
            {
                continue;
            }

            if (distance > detectionRadius)
                continue;

            var weight = 1f;
            var obstacleDirection = pointB - pointA;

            // Inside each other so just use worldPos
            if (distance == 0f)
            {
                obstacleDirection = _transform.GetWorldPosition(xformB) - worldPos;

                // Welp
                if (obstacleDirection == Vector2.Zero)
                {
                    obstacleDirection = _random.NextAngle().ToVec();
                }
            }
            else
            {
                weight = distance / detectionRadius;
            }

            obstacleDirection = offsetRot.RotateVec(obstacleDirection);
            var norm = obstacleDirection.Normalized();
            weight *= 0.25f;

            for (var i = 0; i < InterestDirections; i++)
            {
                var dot = Vector2.Dot(norm, Directions[i]);
                danger[i] = MathF.Max(dot * weight, danger[i]);
            }
        }

        _entSetPool.Return(ents);
    }

    #endregion

    // TODO: Alignment

    // TODO: Cohesion
    private void Blend(NPCSteeringComponent steering, float frameTime, Span<float> interest, Span<float> danger)
    {
        /*
         * Future sloth notes:
         * Pathfinder cleanup:
            - Cleanup whatever the fuck is happening in pathfinder
            - Use Flee for melee behavior / actions and get the seek direction from that rather than bulldozing
            - Must always have a path
            - Path should return the full version + the snipped version
            - Pathfinder needs to do diagonals
            - Next node is either <current node + 1> or <nearest node + 1> (on the full path)
            - If greater than <1.5m distance> repath
         */

        // IDK why I didn't do this sooner but blending is a lot better than lastdir for fixing stuttering.
        const float BlendWeight = 10f;
        var blendValue = Math.Min(1f, frameTime * BlendWeight);

        for (var i = 0; i < InterestDirections; i++)
        {
            var currentInterest = interest[i];
            var lastInterest = steering.Interest[i];
            var interestDiff = (currentInterest - lastInterest) * blendValue;
            steering.Interest[i] = lastInterest + interestDiff;

            var currentDanger = danger[i];
            var lastDanger = steering.Danger[i];
            var dangerDiff = (currentDanger - lastDanger) * blendValue;
            steering.Danger[i] = lastDanger + dangerDiff;
        }
    }
}
