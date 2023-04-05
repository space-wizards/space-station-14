namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    /// <summary>
    /// Finds a generic path from start to end.
    /// </summary>
    public List<Vector2i> GetPath(Vector2i start, Vector2i end, bool diagonal = false)
    {
        if (start == end)
        {
            return new List<Vector2i>();
        }

        var frontier = new PriorityQueue<Vector2i, float>();
        frontier.Enqueue(start, 0f);
        var cameFrom = new Dictionary<Vector2i, Vector2i>();
        var node = start;

        while (frontier.TryDequeue(out node, out _))
        {
            if (node == end)
            {
                break;
            }

            if (diagonal)
            {
                for (var i = 0; i < 8; i++)
                {
                    var direction = (DirectionFlag) i;
                    var neighbor = node + direction.AsDir().ToIntVec();

                    if (!cameFrom.TryAdd(neighbor, node))
                        continue;

                    var gScore = OctileDistance(neighbor, end);
                    frontier.Enqueue(neighbor, gScore);
                }
            }
            else
            {
                for (var i = 0; i < 4; i++)
                {
                    var direction = (DirectionFlag) Math.Pow(2, i);
                    var neighbor = node + direction.AsDir().ToIntVec();

                    if (!cameFrom.TryAdd(neighbor, node))
                        continue;

                    frontier.Enqueue(neighbor, ManhattanDistance(neighbor, end));
                }
            }
        }

        if (node != end)
        {
            return new List<Vector2i>();
        }

        var path = new List<Vector2i>();

        do
        {
            path.Add(node);
            var before = cameFrom[node];
            node = before;
        } while (node != start);

        path.Add(start);
        path.Reverse();
        return path;
    }
}
