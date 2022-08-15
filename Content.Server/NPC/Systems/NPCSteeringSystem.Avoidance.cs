using System.Linq;
using Content.Server.NPC.Components;
using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    // Derived from RVO2 library which uses ORCA (optimal reciprocal collision avoidance).
    // Could also potentially use something force based or RVO or detour crowd.

    public bool CollisionAvoidanceEnabled { get; set; } = true;

    public bool ObstacleAvoidanceEnabled { get; set; } = true;

    private const float Radius = 0.35f;
    private const float RVO_EPSILON = 0.00001f;

    private void InitializeAvoidance()
    {
        var configManager = IoCManager.Resolve<IConfigurationManager>();
        configManager.OnValueChanged(CCVars.NPCCollisionAvoidance, SetCollisionAvoidance);
    }

    private void ShutdownAvoidance()
    {
        var configManager = IoCManager.Resolve<IConfigurationManager>();
        configManager.UnsubValueChanged(CCVars.NPCCollisionAvoidance, SetCollisionAvoidance);
    }

    private void SetCollisionAvoidance(bool obj)
    {
        CollisionAvoidanceEnabled = obj;
    }

    private void CollisionAvoidance((NPCSteeringComponent, ActiveNPCComponent, InputMoverComponent, TransformComponent)[] npcs)
    {
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var rvoQuery = GetEntityQuery<NPCRVOComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var fixturesQuery = GetEntityQuery<FixturesComponent>();

        foreach (var (steering, _, mover, xform) in npcs)
        {
            if (!rvoQuery.TryGetComponent(steering.Owner, out var rvo) ||
                !bodyQuery.TryGetComponent(steering.Owner, out var body))
                continue;

            ComputeNeighbors(mover, rvo, body, xform, xformQuery);
            ComputeVelocity(mover, rvo, body, xform, xformQuery, fixturesQuery);
        }
    }

    private void ComputeNeighbors(InputMoverComponent mover, NPCRVOComponent rvo, PhysicsComponent body, TransformComponent xform, EntityQuery<TransformComponent> xformQuery)
    {
        // Obstacles
        var obstacleRange = rvo.ObstacleTimeHorizon * GetSprintSpeed(mover.Owner) + Radius;
        rvo.ObstacleNeighbors.Clear();
        var mapId = xform.MapID;

        if (ObstacleAvoidanceEnabled)
        {
            foreach (var other in _physics.GetBodiesInRange(mapId, xform.WorldPosition, obstacleRange))
            {
                if (!other.CanCollide ||
                    !other.Hard ||
                    other.BodyType != BodyType.Static ||
                    other.Owner == mover.Owner ||
                    xformQuery.GetComponent(other.Owner).ParentUid != xform.ParentUid)
                    continue;

                rvo.ObstacleNeighbors.Add(other.Owner);
            }
        }

        // Other agents (NPCs / anything else relevant)
        var agentRange = rvo.NeighborRange;
        rvo.AgentNeighbors.Clear();

        if (rvo.MaxNeighbors > 0)
        {
            foreach (var other in _physics.GetBodiesInRange(mapId, xform.WorldPosition, agentRange))
            {
                if (!other.CanCollide ||
                    !other.Hard ||
                    other.BodyType == BodyType.Static ||
                    other.Owner == mover.Owner ||
                    xformQuery.GetComponent(other.Owner).ParentUid != xform.ParentUid)
                    continue;

                rvo.AgentNeighbors.Add(other.Owner);
            }
        }
    }

    private readonly struct ORCAObstacle : IEquatable<ORCAObstacle>
    {
        public readonly bool Convex = true;

        /// <summary>
        /// The vertex
        /// </summary>
        public readonly Vector2 Point;

        /// <summary>
        /// Direction to the next obstacle, normalized.
        /// </summary>
        public readonly Vector2 Direction;

        public ORCAObstacle(Vector2 point, Vector2 direction)
        {
            Point = point;
            Direction = direction;
        }

        public bool Equals(ORCAObstacle other)
        {
            return Convex == other.Convex && Point.Equals(other.Point) && Direction.Equals(other.Direction);
        }

        public override bool Equals(object? obj)
        {
            return obj is ORCAObstacle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Convex, Point, Direction);
        }
    }

    private void ComputeVelocity(
        InputMoverComponent mover,
        NPCRVOComponent rvo,
        PhysicsComponent body,
        TransformComponent xform,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<FixturesComponent> fixturesQuery)
    {
        rvo.OrcaLines.Clear();
        var invTimeHorizonObst = 1f / rvo.ObstacleTimeHorizon;
        var position = xform.LocalPosition;

        var velocity = body.LinearVelocity;
        var radius = 0.35f;

        // Create ORCA lines for obstacles
        foreach (var obstacleEntity in rvo.ObstacleNeighbors)
        {
            var obstacleXform = xformQuery.GetComponent(obstacleEntity);
            var fixtureManager = fixturesQuery.GetComponent(obstacleEntity);
            var transform = new Transform(obstacleXform.LocalPosition, obstacleXform.LocalRotation);

            foreach (var (_, fixture) in fixtureManager.Fixtures)
            {
                var shape = fixture.Shape;
                // TODO: The issue is you don't want every obstacle in a poly so look at RVO.
                var obstacles = new ValueList<ORCAObstacle>();

                switch (shape)
                {
                    case PolygonShape poly:
                        for (var i = 0; i < poly.VertexCount; i++)
                        {
                            var vert = poly.Vertices[i];
                            var nextVert = (poly.Vertices[(i + 1) % poly.VertexCount]);

                            obstacles.Add(new ORCAObstacle(Robust.Shared.Physics.Transform.Mul(transform, vert), (nextVert - vert).Normalized));
                        }

                        break;
                    default:
                        continue;
                }

                for (var i = 0; i < obstacles.Count; i++)
                {
                    var obstacle1 = obstacles[i];
                    var obstacle2 = obstacles[(i + 1) % obstacles.Count];

                    Vector2 relativePosition1 = obstacle1.Point - position;
                    Vector2 relativePosition2 = obstacle2.Point - position;

                    /*
                     * Check if velocity obstacle of obstacle is already taken care
                     * of by previously constructed obstacle ORCA lines.
                     */
                    bool alreadyCovered = false;

                    for (int j = 0; j < rvo.OrcaLines.Count; ++j)
                    {
                        if (Vector2.Determinant(relativePosition1 * invTimeHorizonObst - rvo.OrcaLines[j].Point,
                                rvo.OrcaLines[j].Direction) - invTimeHorizonObst * radius >= -RVO_EPSILON &&
                            Vector2.Determinant(relativePosition2 * invTimeHorizonObst - rvo.OrcaLines[j].Point,
                                rvo.OrcaLines[j].Direction) - invTimeHorizonObst * radius >= -RVO_EPSILON)
                        {
                            alreadyCovered = true;

                            break;
                        }
                    }

                    if (alreadyCovered)
                    {
                        continue;
                    }

                    /* Not yet covered. Check for collisions. */
                    float distSq1 = relativePosition1.LengthSquared;
                    float distSq2 = relativePosition2.LengthSquared;

                    float radiusSq = radius * radius;

                    Vector2 obstacleVector = obstacle2.Point - obstacle1.Point;
                    float s = Vector2.Dot(-relativePosition1, obstacleVector) / obstacleVector.LengthSquared;
                    float distSqLine = (-relativePosition1 - obstacleVector * s).LengthSquared;

                    ORCALine line = new();

                    if (s < 0.0f && distSq1 <= radiusSq)
                    {
                        /* Collision with left vertex. Ignore if non-convex. */
                        if (obstacle1.Convex)
                        {
                            line.Point = new Vector2(0.0f, 0.0f);
                            line.Direction =
                                (new Vector2(-relativePosition1.Y, relativePosition1.X)).Normalized;
                            rvo.OrcaLines.Add(line);
                        }

                        continue;
                    }
                    else if (s > 1.0f && distSq2 <= radiusSq)
                    {
                        /*
                         * Collision with right vertex. Ignore if non-convex or if
                         * it will be taken care of by neighboring obstacle.
                         */
                        if (obstacle2.Convex && Vector2.Determinant(relativePosition2, obstacle2.Direction) >= 0.0f)
                        {
                            line.Point = new Vector2(0.0f, 0.0f);
                            line.Direction =
                                (new Vector2(-relativePosition2.Y, relativePosition2.X)).Normalized;
                            rvo.OrcaLines.Add(line);
                        }

                        continue;
                    }
                    else if (s >= 0.0f && s <= 1.0f && distSqLine <= radiusSq)
                    {
                        /* Collision with obstacle segment. */
                        line.Point = new Vector2(0.0f, 0.0f);
                        line.Direction = -obstacle1.Direction;
                        rvo.OrcaLines.Add(line);

                        continue;
                    }

                    /*
                     * No collision. Compute legs. When obliquely viewed, both legs
                     * can come from a single vertex. Legs extend cut-off line when
                     * non-convex vertex.
                     */

                    Vector2 leftLegDirection, rightLegDirection;

                    if (s < 0.0f && distSqLine <= radiusSq)
                    {
                        /*
                         * Obstacle viewed obliquely so that left vertex
                         * defines velocity obstacle.
                         */
                        if (!obstacle1.Convex)
                        {
                            /* Ignore obstacle. */
                            continue;
                        }

                        obstacle2 = obstacle1;

                        float leg1 = MathF.Sqrt(distSq1 - radiusSq);
                        leftLegDirection = new Vector2(relativePosition1.X * leg1 - relativePosition1.Y * radius,
                            relativePosition1.X * radius + relativePosition1.Y * leg1) / distSq1;
                        rightLegDirection = new Vector2(relativePosition1.X * leg1 + relativePosition1.Y * radius,
                            -relativePosition1.X * radius + relativePosition1.Y * leg1) / distSq1;
                    }
                    else if (s > 1.0f && distSqLine <= radiusSq)
                    {
                        /*
                         * Obstacle viewed obliquely so that
                         * right vertex defines velocity obstacle.
                         */
                        if (!obstacle2.Convex)
                        {
                            /* Ignore obstacle. */
                            continue;
                        }

                        obstacle1 = obstacle2;

                        float leg2 = MathF.Sqrt(distSq2 - radiusSq);
                        leftLegDirection = new Vector2(relativePosition2.X * leg2 - relativePosition2.Y * radius,
                            relativePosition2.X * radius + relativePosition2.Y * leg2) / distSq2;
                        rightLegDirection = new Vector2(relativePosition2.X * leg2 + relativePosition2.Y * radius,
                            -relativePosition2.X * radius + relativePosition2.Y * leg2) / distSq2;
                    }
                    else
                    {
                        /* Usual situation. */
                        if (obstacle1.Convex)
                        {
                            float leg1 = MathF.Sqrt(distSq1 - radiusSq);
                            leftLegDirection =
                                new Vector2(relativePosition1.X * leg1 - relativePosition1.Y * radius,
                                    relativePosition1.X * radius + relativePosition1.Y * leg1) / distSq1;
                        }
                        else
                        {
                            /* Left vertex non-convex; left leg extends cut-off line. */
                            leftLegDirection = -obstacle1.Direction;
                        }

                        if (obstacle2.Convex)
                        {
                            float leg2 = MathF.Sqrt(distSq2 - radiusSq);
                            rightLegDirection =
                                new Vector2(relativePosition2.X * leg2 + relativePosition2.Y * radius,
                                    -relativePosition2.X * radius + relativePosition2.Y * leg2) / distSq2;
                        }
                        else
                        {
                            /* Right vertex non-convex; right leg extends cut-off line. */
                            rightLegDirection = obstacle1.Direction;
                        }
                    }

                    /*
                     * Legs can never point into neighboring edge when convex
                     * vertex, take cutoff-line of neighboring edge instead. If
                     * velocity projected on "foreign" leg, no constraint is added.
                     */

                    ORCAObstacle leftNeighbor = i == 0 ? obstacles[^1] : obstacles[i - 1];

                    bool isLeftLegForeign = false;
                    bool isRightLegForeign = false;

                    if (obstacle1.Convex && Vector2.Determinant(leftLegDirection, -leftNeighbor.Direction) >= 0.0f)
                    {
                        /* Left leg points into obstacle. */
                        leftLegDirection = -leftNeighbor.Direction;
                        isLeftLegForeign = true;
                    }

                    if (obstacle2.Convex && Vector2.Determinant(rightLegDirection, obstacle2.Direction) <= 0.0f)
                    {
                        /* Right leg points into obstacle. */
                        rightLegDirection = obstacle2.Direction;
                        isRightLegForeign = true;
                    }

                    /* Compute cut-off centers. */
                    Vector2 leftCutOff = (obstacle1.Point - position) * invTimeHorizonObst;
                    Vector2 rightCutOff = (obstacle2.Point - position) * invTimeHorizonObst;
                    Vector2 cutOffVector = rightCutOff - leftCutOff;

                    /* Project current velocity on velocity obstacle. */

                    /* Check if current velocity is projected on cutoff circles. */
                    float t = obstacle1.Equals(obstacle2)
                        ? 0.5f
                        : (Vector2.Dot((velocity - leftCutOff), cutOffVector)) / cutOffVector.LengthSquared;

                    float tLeft = Vector2.Dot((velocity - leftCutOff), leftLegDirection);
                    float tRight = Vector2.Dot((velocity - rightCutOff), rightLegDirection);

                    if ((t < 0.0f && tLeft < 0.0f) || (obstacle1.Equals(obstacle2) && tLeft < 0.0f && tRight < 0.0f))
                    {
                        /* Project on left cut-off circle. */
                        Vector2 unitW = (velocity - leftCutOff).Normalized;

                        line.Direction = new Vector2(unitW.Y, -unitW.X);
                        line.Point = leftCutOff + unitW * invTimeHorizonObst * radius;
                        rvo.OrcaLines.Add(line);

                        continue;
                    }
                    else if (t > 1.0f && tRight < 0.0f)
                    {
                        /* Project on right cut-off circle. */
                        Vector2 unitW = (velocity - rightCutOff).Normalized;

                        line.Direction = new Vector2(unitW.Y, -unitW.X);
                        line.Point = rightCutOff + unitW * radius * invTimeHorizonObst;
                        rvo.OrcaLines.Add(line);

                        continue;
                    }

                    /*
                     * Project on left leg, right leg, or cut-off line, whichever is
                     * closest to velocity.
                     */
                    float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1.Equals(obstacle2))
                        ? float.PositiveInfinity
                        : (velocity - (leftCutOff + cutOffVector * t)).LengthSquared;
                    float distSqLeft = tLeft < 0.0f
                        ? float.PositiveInfinity
                        : (velocity - (leftCutOff + leftLegDirection * tLeft)).LengthSquared;
                    float distSqRight = tRight < 0.0f
                        ? float.PositiveInfinity
                        : (velocity - (rightCutOff + rightLegDirection * tRight)).LengthSquared;

                    if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                    {
                        /* Project on cut-off line. */
                        line.Direction = -obstacle1.Direction;
                        line.Point = leftCutOff + new Vector2(-line.Direction.Y, line.Direction.X) * radius * invTimeHorizonObst;
                        rvo.OrcaLines.Add(line);

                        continue;
                    }

                    if (distSqLeft <= distSqRight)
                    {
                        /* Project on left leg. */
                        if (isLeftLegForeign)
                        {
                            continue;
                        }

                        line.Direction = leftLegDirection;
                        line.Point = leftCutOff + new Vector2(-line.Direction.Y, line.Direction.X) * radius * invTimeHorizonObst;
                        rvo.OrcaLines.Add(line);

                        continue;
                    }

                    /* Project on right leg. */
                    if (isRightLegForeign)
                    {
                        continue;
                    }

                    line.Direction = -rightLegDirection;
                    line.Point = rightCutOff + new Vector2(-line.Direction.Y, line.Direction.X) * radius * invTimeHorizonObst;
                    rvo.OrcaLines.Add(line);
                }
            }
        }

        var numObstLines = rvo.OrcaLines.Count;
        var invTimeHorizon = 1f / rvo.TimeHorizon;

        // Create agent ORCA lines
        foreach (var other in rvo.AgentNeighbors)
        {
            var otherXform = xformQuery.GetComponent(other);
            var otherBody = Comp<PhysicsComponent>(other);

            Vector2 relativePosition = otherXform.LocalPosition - xform.LocalPosition;
            Vector2 relativeVelocity = body.LinearVelocity - otherBody.LinearVelocity;
            float distSq = relativePosition.LengthSquared;
            float combinedRadius = Radius + Radius;
            float combinedRadiusSq = combinedRadius * combinedRadius;

            ORCALine line = new();
            Vector2 u;

            if (distSq > combinedRadiusSq)
            {
                /* No collision. */
                Vector2 w = relativeVelocity - relativePosition * invTimeHorizon;

                /* Vector from cutoff center to relative velocity. */
                float wLengthSq = w.LengthSquared;
                float dotProduct1 = Vector2.Dot(relativePosition, w);

                if (dotProduct1 < 0.0f && dotProduct1 * dotProduct1 > combinedRadiusSq * wLengthSq)
                {
                    /* Project on cut-off circle. */
                    float wLength = MathF.Sqrt(wLengthSq);
                    Vector2 unitW = w / wLength;

                    line.Direction = new Vector2(unitW.Y, -unitW.X);
                    u = unitW * (combinedRadius * invTimeHorizon - wLength);
                }
                else
                {
                    /* Project on legs. */
                    float leg = MathF.Sqrt(distSq - combinedRadiusSq);

                    if (Vector2.Determinant(relativePosition, w) > 0.0f)
                    {
                        /* Project on left leg. */
                        line.Direction = new Vector2(relativePosition.X * leg - relativePosition.Y * combinedRadius, relativePosition.X * combinedRadius + relativePosition.Y * leg) / distSq;
                    }
                    else
                    {
                        /* Project on right leg. */
                        line.Direction = -new Vector2(relativePosition.X * leg + relativePosition.Y * combinedRadius, -relativePosition.X * combinedRadius + relativePosition.Y * leg) / distSq;
                    }

                    float dotProduct2 = Vector2.Dot(relativeVelocity, line.Direction);
                    u = line.Direction * dotProduct2 - relativeVelocity;
                }
            }
            else
            {
                /* Collision. Project on cut-off circle of time timeStep. */
                float invTimeStep = 1.0f / (float) _timing.TickPeriod.TotalSeconds;

                /* Vector from cutoff center to relative velocity. */
                Vector2 w = relativeVelocity - relativePosition * invTimeStep;

                float wLength = w.Length;
                Vector2 unitW = w / wLength;

                line.Direction = new Vector2(unitW.Y, -unitW.X);
                u = unitW * (combinedRadius * invTimeStep - wLength);
            }

            line.Point = body.LinearVelocity + u * 0.5f;
            rvo.OrcaLines.Add(line);
        }

        var maxSpeed = GetSprintSpeed(mover.Owner);
        var newVelocity = Vector2.Zero;

        int lineFail = linearProgram2(rvo.OrcaLines, maxSpeed, mover.CurTickSprintMovement * maxSpeed, false, ref newVelocity);

        if (lineFail < rvo.OrcaLines.Count)
        {
            linearProgram3(rvo.OrcaLines, numObstLines, lineFail, maxSpeed, ref newVelocity);
        }

        // We're provided a velocity between 0 and max but we let physics handle the actual movement so just need the input vector.
        mover.CurTickSprintMovement = newVelocity / maxSpeed;
    }

    /**
         * <summary>Solves a one-dimensional linear program on a specified line
         * subject to linear constraints defined by lines and a circular
         * constraint.</summary>
         *
         * <returns>True if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="lineNo">The specified line constraint.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private bool linearProgram1(IList<ORCALine> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            float dotProduct = Vector2.Dot(lines[lineNo].Point, lines[lineNo].Direction);
            float discriminant = dotProduct * dotProduct + radius * radius - lines[lineNo].Point.LengthSquared;

            if (discriminant < 0.0f)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            float sqrtDiscriminant = MathF.Sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                float denominator = Vector2.Determinant(lines[lineNo].Direction, lines[i].Direction);
                float numerator = Vector2.Determinant(lines[i].Direction, lines[lineNo].Point - lines[i].Point);

                if (MathF.Abs(denominator) <= RVO_EPSILON)
                {
                    /* Lines lineNo and i are (almost) parallel. */
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    /* Line i bounds line lineNo on the right. */
                    tRight = Math.Min(tRight, t);
                }
                else
                {
                    /* Line i bounds line lineNo on the left. */
                    tLeft = Math.Max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (directionOpt)
            {
                /* Optimize direction. */
                if (Vector2.Dot(optVelocity, lines[lineNo].Direction) > 0.0f)
                {
                    /* Take right extreme. */
                    result = lines[lineNo].Point + lines[lineNo].Direction * tRight;
                }
                else
                {
                    /* Take left extreme. */
                    result = lines[lineNo].Point + lines[lineNo].Direction * tLeft;
                }
            }
            else
            {
                /* Optimize closest point. */
                float t = Vector2.Dot(lines[lineNo].Direction, (optVelocity - lines[lineNo].Point));

                if (t < tLeft)
                {
                    result = lines[lineNo].Point + lines[lineNo].Direction * tLeft;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].Point + lines[lineNo].Direction * tRight;
                }
                else
                {
                    result = lines[lineNo].Point + lines[lineNo].Direction * t;
                }
            }

            return true;
        }

     /**
     * <summary>Solves a two-dimensional linear program subject to linear
     * constraints defined by lines and a circular constraint.</summary>
     *
     * <returns>The number of the line it fails on, and the number of lines
     * if successful.</returns>
     *
     * <param name="lines">Lines defining the linear constraints.</param>
     * <param name="radius">The radius of the circular constraint.</param>
     * <param name="optVelocity">The optimization velocity.</param>
     * <param name="directionOpt">True if the direction should be optimized.
     * </param>
     * <param name="result">A reference to the result of the linear program.
     * </param>
     */
    private int linearProgram2(IList<ORCALine> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
    {
        if (directionOpt)
        {
            /*
             * Optimize direction. Note that the optimization velocity is of
             * unit length in this case.
             */
            result = optVelocity * radius;
        }
        else if (optVelocity.LengthSquared > radius * radius)
        {
            /* Optimize closest point and outside circle. */
            result = optVelocity.Normalized * radius;
        }
        else
        {
            /* Optimize closest point and inside circle. */
            result = optVelocity;
        }

        for (int i = 0; i < lines.Count; ++i)
        {
            if (Vector2.Determinant(lines[i].Direction, lines[i].Point - result) > 0.0f)
            {
                /* Result does not satisfy constraint i. Compute new optimal result. */
                Vector2 tempResult = result;
                if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                {
                    result = tempResult;

                    return i;
                }
            }
        }

        return lines.Count;
    }

    /**
     * <summary>Solves a two-dimensional linear program subject to linear
     * constraints defined by lines and a circular constraint.</summary>
     *
     * <param name="lines">Lines defining the linear constraints.</param>
     * <param name="numObstLines">Count of obstacle lines.</param>
     * <param name="beginLine">The line on which the 2-d linear program
     * failed.</param>
     * <param name="radius">The radius of the circular constraint.</param>
     * <param name="result">A reference to the result of the linear program.
     * </param>
     */
    private void linearProgram3(IList<ORCALine> lines, int numObstLines, int beginLine, float radius, ref Vector2 result)
    {
        float distance = 0.0f;

        for (int i = beginLine; i < lines.Count; ++i)
        {
            if (Vector2.Determinant(lines[i].Direction, lines[i].Point - result) > distance)
            {
                /* Result does not satisfy constraint of line i. */
                IList<ORCALine> projLines = new List<ORCALine>();
                for (int ii = 0; ii < numObstLines; ++ii)
                {
                    projLines.Add(lines[ii]);
                }

                for (int j = numObstLines; j < i; ++j)
                {
                    ORCALine line = new();

                    float determinant = Vector2.Determinant(lines[i].Direction, lines[j].Direction);

                    if (MathF.Abs(determinant) <= RVO_EPSILON)
                    {
                        /* Line i and line j are parallel. */
                        if (Vector2.Dot(lines[i].Direction, lines[j].Direction) > 0.0f)
                        {
                            /* Line i and line j point in the same direction. */
                            continue;
                        }
                        else
                        {
                            /* Line i and line j point in opposite direction. */
                            line.Point = (lines[i].Point + lines[j].Point) * 0.5f;
                        }
                    }
                    else
                    {
                        line.Point = lines[i].Point + (lines[i].Direction * Vector2.Determinant(lines[j].Direction, lines[i].Point - lines[j].Point) / determinant);
                    }

                    line.Direction = (lines[j].Direction - lines[i].Direction).Normalized;
                    projLines.Add(line);
                }

                Vector2 tempResult = result;
                if (linearProgram2(projLines, radius, new Vector2(-lines[i].Direction.Y, lines[i].Direction.X), true, ref result) < projLines.Count)
                {
                    /*
                     * This should in principle not happen. The result is by
                     * definition already in the feasible region of this
                     * linear program. If it fails, it is due to small
                     * floating point error, and the current result is kept.
                     */
                    result = tempResult;
                }

                distance = Vector2.Determinant(lines[i].Direction, lines[i].Point - result);
            }
        }
    }
}

public struct ORCALine
{
    public Vector2 Point;
    public Vector2 Direction;
}
