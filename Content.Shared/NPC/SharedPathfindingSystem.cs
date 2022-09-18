namespace Content.Shared.NPC;

public abstract class SharedPathfindingSystem : EntitySystem
{
    /// <summary>
    /// This is equivalent to agent radii for navmeshes. In our case it's preferable that things are cleanly
    /// divisible per tile so we'll make sure it works as a discrete number.
    /// </summary>
    public const int SubStep = 4;

    public const int ChunkSize = 8;

    /// <summary>
    /// Something we need to consider is that we're chunk-based. The issue with this is if the border of one chunk
    /// has a bunch of obstacles then the other neighbor doesn't know about it. To resolve this we expand
    /// out slightly
    /// </summary>
    public const int ExpansionSize = 1;

    /// <summary>
    /// We won't do points on edges so we'll offset them slightly.
    /// </summary>
    public const float StepOffset = 1f / SubStep / 2f;

    public Vector2i GetPointCoordinate(Vector2 origin)
    {
        return new Vector2i((int) ((origin.X - StepOffset - ExpansionSize) * SubStep), (int) ((origin.Y - StepOffset - ExpansionSize) * SubStep));
    }

    public Vector2 GetCoordinate(Vector2i chunk, Vector2i index)
    {
        return new Vector2(index.X, index.Y) / SubStep - ExpansionSize + (chunk) * ChunkSize + StepOffset;
    }
}
