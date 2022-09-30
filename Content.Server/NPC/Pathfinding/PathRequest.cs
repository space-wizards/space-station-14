using System.Threading;
using System.Threading.Tasks;
using Content.Shared.NPC;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Pathfinding;

/// <summary>
/// Stores the in-progress data of a pathfinding request.
/// </summary>
public abstract class PathRequest
{
    public EntityCoordinates Start;

    public Task<PathResult> Task => Tcs.Task;
    public readonly TaskCompletionSource<PathResult> Tcs;

    public Queue<PathPoly> Polys = new();

    public bool Started = false;

    #region Pathfinding state

    public readonly Stopwatch Stopwatch = new();
    public PriorityQueue<ValueTuple<float, PathPoly>> Frontier = default!;
    public readonly Dictionary<PathPoly, float> CostSoFar = new();
    public readonly Dictionary<PathPoly, PathPoly> CameFrom = new();

    #endregion

    #region Data

    public readonly PathFlags Flags;
    public readonly float Range;
    public readonly int CollisionLayer;
    public readonly int CollisionMask;

    #endregion

    public PathRequest(EntityCoordinates start, PathFlags flags, float range, int layer, int mask, CancellationToken cancelToken)
    {
        Start = start;
        Flags = flags;
        Range = range;
        CollisionLayer = layer;
        CollisionMask = mask;
        Tcs = new TaskCompletionSource<PathResult>(cancelToken);
    }
}

public sealed class AStarPathRequest : PathRequest
{
    public EntityCoordinates End;

    public AStarPathRequest(
        EntityCoordinates start,
        EntityCoordinates end,
        PathFlags flags,
        float range,
        int layer,
        int mask,
        CancellationToken cancelToken) : base(start, flags, range, layer, mask, cancelToken)
    {
        End = end;
    }
}

public sealed class BFSPathRequest : PathRequest
{
    /// <summary>
    /// How far away we're allowed to expand in distance.
    /// </summary>
    public float ExpansionRange;

    /// <summary>
    /// How many nodes we're allowed to expand
    /// </summary>
    public int ExpansionLimit;

    public BFSPathRequest(
        float expansionRange,
        int expansionLimit,
        EntityCoordinates start,
        PathFlags flags,
        float range,
        int layer,
        int mask,
        CancellationToken cancelToken) : base(start, flags, range, layer, mask, cancelToken)
        {
            ExpansionRange = expansionRange;
            ExpansionLimit = expansionLimit;
        }
}

/// <summary>
/// Stores the final result of a pathfinding request
/// </summary>
public sealed class PathResultEvent
{
    public PathResult Result;
    public readonly Queue<PathPoly> Path;

    public PathResultEvent(PathResult result, Queue<PathPoly> path)
    {
        Result = result;
        Path = path;
    }
}
