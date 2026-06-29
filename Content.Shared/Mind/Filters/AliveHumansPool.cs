using Content.Shared.Objectives.Systems;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind pool that uses <see cref="TargetSystem.AddAliveHumans"/>.
/// </summary>
public sealed partial class AliveHumansPool : MindPool
{
    public override void FindMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude, IEntityManager entMan, TargetSystem targetSystem)
    {
        targetSystem.AddAliveHumans(minds, exclude);
    }
}
