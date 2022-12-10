using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.NPC;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
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
            ref NPCSteeringContext context,
            Vector2[] directions,
            EntityQuery<PhysicsComponent> bodyQuery,
            EntityQuery<MovementSpeedModifierComponent> modifierQuery,
            float frameTime)
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
            else
            {
                arrivalDistance = SharedInteractionSystem.InteractionRange - 0.8f;
            }

            // Check if mapids match.
            var targetMap = targetCoordinates.ToMap(EntityManager);
            var ourMap = ourCoordinates.ToMap(EntityManager);

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
                if (steering.CurrentPath.TryPeek(out var node))
                {
                    var status = TryHandleFlags(steering, node, bodyQuery);

                    // TODO: Need to handle re-pathing in case the target moves around.
                    switch (status)
                    {
                        case SteeringObstacleStatus.Completed:
                            break;
                        case SteeringObstacleStatus.Failed:
                            // TODO: Blacklist the poly for next query
                            steering.Status = SteeringStatus.NoPath;
                            return false;
                        case SteeringObstacleStatus.Continuing:
                            CheckPath(steering, xform, needsPath, distance);
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // Otherwise it's probably regular pathing so just keep going a bit more to get to tile centre
                if (direction.Length <= TileTolerance)
                {
                    // It was just a node, not the target, so grab the next destination (either the target or next node).
                    if (steering.CurrentPath.Count > 0)
                    {
                        steering.CurrentPath.Dequeue();

                        // Alright just adjust slightly and grab the next node so we don't stop moving for a tick.
                        // TODO: If it's the last node just grab the target instead.
                        targetCoordinates = GetTargetCoordinates(steering);
                        targetMap = targetCoordinates.ToMap(EntityManager);

                        // Can't make it again.
                        if (ourMap.MapId != targetMap.MapId)
                        {
                            SetDirection(mover, steering, Vector2.Zero);
                            steering.Status = SteeringStatus.NoPath;
                            return false;
                        }

                        // Gonna resume now business as usual
                        direction = targetMap.Position - ourMap.Position;
                    }
                    else
                    {
                        // This probably shouldn't happen as we check above but eh.
                        steering.Status = SteeringStatus.NoPath;
                        return false;
                    }
                }
            }

            // Do we have no more nodes to follow OR has the target moved sufficiently? If so then re-path.
            if (!needsPath)
            {
                needsPath = steering.CurrentPath.Count == 0 || (steering.CurrentPath.Peek().Data.Flags & PathfindingBreadcrumbFlag.Invalid) != 0x0;
            }

            // TODO: Probably need partial planning support i.e. patch from the last node to where the target moved to.
            CheckPath(steering, xform, needsPath, distance);

            if (steering is { Pathfind: true, CurrentPath.Count: 0 })
            {
                return true;
            }

            modifierQuery.TryGetComponent(steering.Owner, out var modifier);
            var moveSpeed = GetSprintSpeed(steering.Owner, modifier);

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

            for (var i = 0; i < InterestDirections; i++)
            {
                var result = Vector2.Dot(norm, directions[i]);
                var adjustedResult = (result + 1f) * 0.5f * weight;

                context.Interest[i] = MathF.Max(context.Interest[i], adjustedResult);
            }

            // Prefer our current direction
            if (weight > 0f && body.LinearVelocity.LengthSquared > 0f)
            {
                var sameDirectionWeight = 0.1f;
                norm = body.LinearVelocity.Normalized;

                for (var i = 0; i < InterestDirections; i++)
                {
                    var result = Vector2.Dot(norm, directions[i]);
                    var adjustedResult = (result + 1f) * 0.5f * weight;

                    context.Interest[i] = MathF.Max(context.Interest[i], adjustedResult);
                }
            }

            return true;
        }

        private void CheckPath(NPCSteeringComponent steering, TransformComponent xform, bool needsPath, float targetDistance)
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
                RequestPath(steering, xform, targetDistance);
            }
        }

        /// <summary>
        /// We may be pathfinding and moving at the same time in which case early nodes may be out of date.
        /// </summary>
        public void PrunePath(MapCoordinates mapCoordinates, Vector2 direction, Queue<PathPoly> nodes)
        {
            if (nodes.Count == 0)
                return;

            // Prune the first node as it's irrelevant.
            nodes.Dequeue();

            while (nodes.TryPeek(out var node))
            {
                if (!node.Data.IsFreeSpace)
                    break;

                var nodeMap = node.Coordinates.ToMap(EntityManager);

                // If any nodes are 'behind us' relative to the target we'll prune them.
                // This isn't perfect but should fix most cases of stutter stepping.
                if (nodeMap.MapId == mapCoordinates.MapId &&
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
            else
            {
                return value >= minValue ? 1f : 0f;
            }
        }

        #endregion

        #region Static Avoidance

        /// <summary>
        /// Tries to avoid static blockers such as walls.
        /// </summary>
        private void StaticAvoid(
            EntityUid uid,
            Angle offsetRot,
            Vector2 worldPos,
            float agentRadius,
            float moveSpeed,
            PhysicsComponent body,
            TransformComponent xform,
            ref NPCSteeringContext context,
            List<Vector2> dangerPoints,
            Vector2[] directions,
            EntityQuery<PhysicsComponent> bodyQuery,
            EntityQuery<TransformComponent> xformQuery)
        {
            var detectionRadius = agentRadius * 1.5f;
            var away = new Vector2();

            foreach (var ent in _lookup.GetEntitiesInRange(uid, detectionRadius, LookupFlags.Static))
            {
                // TODO: If we can access the door or smth.
                if (ent == uid ||
                    !bodyQuery.TryGetComponent(ent, out var otherBody) ||
                    !otherBody.Hard ||
                    !otherBody.CanCollide ||
                    ((body.CollisionMask & otherBody.CollisionLayer) == 0x0 &&
                    (body.CollisionLayer & otherBody.CollisionMask) == 0x0))
                {
                    continue;
                }

                if (!_physics.TryGetNearestPoints(uid, ent, out var pointA, out var pointB, xform, xformQuery.GetComponent(ent)))
                {
                    continue;
                }

                var obstacleDirection = offsetRot.RotateVec(pointB - pointA);
                var obstacleDistance = obstacleDirection.Length;

                if (obstacleDistance > detectionRadius || obstacleDistance == 0f)
                    continue;

                var weight = obstacleDistance <= agentRadius ? 1f : (detectionRadius - obstacleDistance) / detectionRadius;
                var norm = obstacleDirection.Normalized;
                dangerPoints.Add(pointB);

                for (var i = 0; i < InterestDirections; i++)
                {
                    var result = Vector2.Dot(norm, directions[i]);
                    var factor = 1f - (displacement.Length / agentRadius);
                    away += displacement.Normalized * factor;
                }
            }
        }

        #endregion

        #region Dynamic Avoidance

        /// <summary>
        /// Tries to avoid mobs of the same faction.
        /// </summary>
        private void DynamicAvoid(
            EntityUid uid,
            Vector2 worldPos,
            float agentRadius,
            PhysicsComponent body,
            TransformComponent xform,
            ref NPCSteeringContext context,
            EntityQuery<PhysicsComponent> bodyQuery,
            EntityQuery<TransformComponent> xformQuery)
        {
            var avoidDistance = agentRadius;
            var away = new Vector2();

            foreach (var ent in _lookup.GetEntitiesInRange(uid, avoidDistance, LookupFlags.Dynamic))
            {
                // TODO: If we can access the door or smth.
                if (ent == uid ||
                    !bodyQuery.TryGetComponent(ent, out var otherBody) ||
                    !otherBody.Hard ||
                    !otherBody.CanCollide ||
                    ((body.CollisionMask & otherBody.CollisionLayer) == 0x0 &&
                     (body.CollisionLayer & otherBody.CollisionMask) == 0x0) ||
                    // TODO: Internal resolves
                    !_faction.IsFriendly(uid, ent))
                {
                    continue;
                }

                var xformB = xformQuery.GetComponent(ent);

                // TODO: More queries and shit.
                if (!_physics.TryGetNearestPoints(uid, ent, out var pointA, out var pointB, xform,
                        xformB))
                {
                    continue;
                }

                var displacement = worldPos - xformB.WorldPosition;

                // Pick a random direction then
                if (displacement.LengthSquared == 0f)
                {
                    away += _random.NextAngle().ToVec();
                }
                // Overlap
                else if (pointB.Equals(pointA))
                {
                    var factor = 1f - (displacement.Length / agentRadius);
                    away += displacement.Normalized * factor;
                }
            }

            context.Seek(away, 1f);
        }

        #endregion
}
