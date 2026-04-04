namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind pool that uses <see cref="SharedMindSystem.AddAliveHumans"/>.
/// </summary>
public sealed partial class AliveHumansPool : MindPool
{
    public override void FindMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        mindSys.AddAliveHumans(minds, exclude);
    }
}
