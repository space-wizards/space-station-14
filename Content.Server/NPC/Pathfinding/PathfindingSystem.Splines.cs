namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    /// <summary>
    /// Gets a spline path from start to end.
    /// </summary>
    public List<Vector2i> GetSplinePath(SplinePathArgs args)
    {

    }

    public record struct SplinePathArgs
    {
        public Vector2i Start;
        public Vector2i End;
    }
}
