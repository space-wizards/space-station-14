namespace Content.Shared.NPC;

public abstract class SharedPathfindingSystem : EntitySystem
{
    /// <summary>
    /// This is equivalent to agent radii for navmeshes. In our case it's preferable that things are cleanly
    /// divisible per tile so we'll make sure it works as a discrete number.
    /// </summary>
    public const int SubStep = 4;

    public const int ChunkSize = 4;
}
