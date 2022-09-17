namespace Content.Shared.NPC;

public abstract class SharedPathfindingSystem : EntitySystem
{
    /// <summary>
    /// This is equivalent to agent radii for navmeshes. In our case it's preferable that things are cleanly
    /// divisible per tile so we'll make sure it works as a discrete number.
    /// </summary>
    public const int SubStep = 4;

    public const int ChunkSize = 4;

    /// <summary>
    /// We won't do points on edges so we'll offset them slightly.
    /// </summary>
    public const float StepOffset = 1f / SubStep / 2f;

    public Vector2i GetPointCoordinate(Vector2 origin)
    {
        return new Vector2i((int) ((origin.X - StepOffset) * SubStep), (int) ((origin.Y - StepOffset) * SubStep));
    }

    public Vector2 GetCoordinate(Vector2i origin)
    {
        return new Vector2(origin.X / (float) SubStep + StepOffset, origin.Y / (float) SubStep + StepOffset);
    }
}
