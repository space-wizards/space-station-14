namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind pool that uses <see cref="SharedMindSystem.AddAliveAi"/>.
/// </summary>
public sealed partial class AliveAiPool : MindPool
{
    public override void FindMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        mindSys.AddAliveAi(minds, exclude);
    }
}
