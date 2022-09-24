using Content.Shared.NPC;
using Robust.Shared.Map;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    /*
     * Code that is common to all pathfinding methods.
     */

    /// <summary>
    /// Maximum amount of nodes we're allowed to expand.
    /// </summary>
    private const int NodeLimit = 200;

    private sealed class PathComparer : IComparer<ValueTuple<float, PathPoly>>
    {
        public int Compare((float, PathPoly) x, (float, PathPoly) y)
        {
            return y.Item1.CompareTo(x.Item1);
        }
    }

    private static readonly PathComparer PathPolyComparer = new();

    private Queue<PathPoly> ReconstructPath(Dictionary<PathPoly, PathPoly> path, PathPoly currentNodeRef)
    {
        var running = new Stack<PathPoly>();
        running.Push(currentNodeRef);
        while (path.ContainsKey(currentNodeRef))
        {
            var previousCurrent = currentNodeRef;
            currentNodeRef = path[currentNodeRef];
            path.Remove(previousCurrent);
            running.Push(currentNodeRef);
        }

        var result = new Queue<PathPoly>(running);
        return result;
    }

    private float GetTileCost(PathRequest request, PathPoly start, PathPoly end)
    {
        var modifier = 1f;

        if ((request.CollisionLayer & end.Data.CollisionMask) != 0x0 ||
            (request.CollisionMask & end.Data.CollisionLayer) != 0x0)
        {
            var isDoor = (end.Data.Flags & PathfindingBreadcrumbFlag.Door) != 0x0;
            var isAccess = (end.Data.Flags & PathfindingBreadcrumbFlag.Access) != 0x0;

            // TODO: Handling power + door prying
            // Door we should be able to open
            if (isDoor && !isAccess)
            {
                modifier += 0.5f;
            }
            // Door we can force open one way or another
            else if (isDoor && isAccess && (request.Flags & PathFlags.Prying) != 0x0)
            {
                modifier += 4f;
            }
            else if ((request.Flags & PathFlags.Smashing) != 0x0 && end.Data.Damage > 0f)
            {
                modifier += 7f + end.Data.Damage / 100f;
            }
            else
            {
                return 0f;
            }
        }

        return modifier * OctileDistance(end, start);
    }
}
