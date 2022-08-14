using Content.Server.NPC.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    // Derived from RVO2 library which uses ORCA (optimal reciprocal collision avoidance).
    // Could also potentially use something force based or RVO or detour crowd.

    public bool CollisionAvoidanceEnabled { get; set; }= true;

    private const float Radius = 0.35f;
    private const float RVO_EPSILON = 0.00001f;

    private void InitializeAvoidance()
    {

    }

    private void CollisionAvoidance((NPCSteeringComponent, ActiveNPCComponent, InputMoverComponent, TransformComponent)[] npcs)
    {
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var rvoQuery = GetEntityQuery<NPCRVOComponent>();

        foreach (var (steering, _, mover, xform) in npcs)
        {
            if (!rvoQuery.TryGetComponent(steering.Owner, out var rvo) ||
                !bodyQuery.TryGetComponent(steering.Owner, out var body))
                continue;

            ComputeNeighbors(mover, rvo, body, xform);
            ComputeVelocity(mover, rvo, body, xform);
        }
    }

    private void ComputeNeighbors(InputMoverComponent mover, NPCRVOComponent rvo, PhysicsComponent body, TransformComponent xform)
    {
        // Obstacles
        var obstacleRange = rvo.ObstacleTimeHorizon * GetSprintSpeed(mover.Owner) + Radius;
        rvo.ObstacleNeighbors.Clear();

        var mapId = xform.MapID;

        foreach (var other in _physics.GetBodiesInRange(mapId, xform.WorldPosition, obstacleRange))
        {
            if (other.BodyType != BodyType.Static ||
                other.Owner == mover.Owner ||
                Transform(other.Owner).ParentUid != xform.ParentUid)
                continue;

            rvo.ObstacleNeighbors.Add(other.Owner);
        }

        // Other agents (NPCs / anything else relevant)
        var agentRange = rvo.NeighborRange;
        rvo.AgentNeighbors.Clear();

        if (rvo.MaxNeighbors > 0)
        {
            foreach (var other in _physics.GetBodiesInRange(mapId, xform.WorldPosition, agentRange))
            {
                if (other.BodyType == BodyType.Static ||
                    other.Owner == mover.Owner ||
                    Transform(other.Owner).ParentUid != xform.ParentUid)
                    continue;

                rvo.AgentNeighbors.Add(other.Owner);
            }
        }
    }

    private void ComputeVelocity(InputMoverComponent mover, NPCRVOComponent rvo, PhysicsComponent body, TransformComponent xform)
    {
        rvo.OrcaLines.Clear();
        var invTimeHorizonObstacle = 1f / rvo.ObstacleTimeHorizon;

        // Create ORCA lines for obstacles
        // TODO:

        var numObstLines = rvo.OrcaLines.Count;
        var invTimeHorizon = 1f / rvo.TimeHorizon;

        // Create agent ORCA lines
        foreach (var other in rvo.AgentNeighbors)
        {
            var otherXform = Transform(other);
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
