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
public sealed class PathRequest
{
    public EntityCoordinates Start;
    public EntityCoordinates End;

    public Task<PathResult> Task => Tcs.Task;
    public readonly TaskCompletionSource<PathResult> Tcs;

    public Queue<PathPoly> Polys = default!;
    public Queue<EntityCoordinates> Path = default!;

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

    public PathRequest(EntityCoordinates start, EntityCoordinates end, PathFlags flags, float range, int layer, int mask, CancellationToken cancelToken)
    {
        Start = start;
        End = end;
        Flags = flags;
        Range = range;
        CollisionLayer = layer;
        CollisionMask = mask;
        Tcs = new TaskCompletionSource<PathResult>(cancelToken);
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
