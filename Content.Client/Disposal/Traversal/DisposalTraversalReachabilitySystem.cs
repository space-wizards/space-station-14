using Content.Shared.Disposal.Traversal;

namespace Content.Client.Disposal.Traversal;

/// <summary>
/// Provides client-side reachability queries for traversal pipe visualization.
/// </summary>
public sealed partial class DisposalTraversalReachabilitySystem : EntitySystem
{
    [Dependency] private DisposalTraversalSystem _traversal = default!;

    private static readonly Direction[] CardinalDirections =
    {
        Direction.North,
        Direction.East,
        Direction.South,
        Direction.West,
    };

    /// <summary>
    /// Returns every traversal segment reachable from the given start segment for the holder's current network state.
    /// </summary>
    public HashSet<EntityUid> GetReachableTubes(Entity<DisposalTraversalHolderComponent?> holder, EntityUid start)
    {
        if (!Resolve(holder, ref holder.Comp, false))
            return [];

        var reachable = new HashSet<EntityUid> { start };
        var queue = new Queue<EntityUid>();
        queue.Enqueue(start);

        while (queue.TryDequeue(out var tube))
        {
            foreach (var direction in CardinalDirections)
            {
                var next = _traversal.NextTubeFor(holder, tube, direction);
                if (next == null || !reachable.Add(next.Value))
                    continue;

                queue.Enqueue(next.Value);
            }
        }

        return reachable;
    }
}
