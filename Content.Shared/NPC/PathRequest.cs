using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.NPC;

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

    public Stopwatch Stopwatch = new();

    public bool Started = false;

    public PathRequest(EntityCoordinates start, EntityCoordinates end, PathFlags flags, CancellationToken cancelToken)
    {
        Start = start;
        End = end;
        Flags = flags;
        Tcs = new TaskCompletionSource<PathResult>(cancelToken);
    }
}

public enum PathResult : byte
{
    NoPath,
    PartialPath,
    Path,
}

[Flags]
public enum PathFlags : byte
{
    None = 0,

    /// <summary>
    /// Do we have any form of access.
    /// </summary>
    Access = 1 << 0,

    /// <summary>
    /// Can we pry airlocks if necessary.
    /// </summary>
    Prying = 1 << 1,

    /// <summary>
    /// Can stuff like walls be broken.
    /// </summary>
    Smashing = 1 << 2,
}
