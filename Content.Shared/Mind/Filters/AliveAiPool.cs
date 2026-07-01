using Content.Shared.Objectives.Systems;
using Content.Shared.Silicons.StationAi;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind pool that uses <see cref="TargetSystem.AddAliveAi"/>.
/// </summary>
public sealed partial class AliveAiPool : MindPool
{
    public override void FindMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude, IEntityManager entMan, TargetSystem targetSys)
    {
        var aiSys = entMan.System<SharedStationAiSystem>();
        aiSys.AddAliveAis(minds, exclude);
    }
}
