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

    public PathFlags Flags;

    public Task Task => Tcs.Task;
    public readonly TaskCompletionSource<PathResult> Tcs;

    public Queue<PathPoly> Polys = default!;

    public bool Started = false;

    #region Pathfinding state

    public readonly Stopwatch Stopwatch = new();
    public PriorityQueue<ValueTuple<float, PathPoly>> Frontier = default!;
    public readonly Dictionary<PathPoly, float> CostSoFar = new();
    public readonly Dictionary<PathPoly, PathPoly> CameFrom = new();

    #endregion

    #region Data

    public int CollisionLayer;
    public int CollisionMask;

    #endregion

    public PathRequest(EntityCoordinates start, EntityCoordinates end, PathFlags flags, CancellationToken cancelToken)
    {
        Start = start;
        End = end;
        Flags = flags;
        Tcs = new TaskCompletionSource<PathResult>(cancelToken);
    }
}