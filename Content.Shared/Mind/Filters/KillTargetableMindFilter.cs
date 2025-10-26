using Content.Shared.Roles.Jobs;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind filter that checks if the mind can be a kill objective target for traitors.
/// </summary>
public sealed partial class KillTargetableMindFilter : MindFilter
{
    protected override bool ShouldRemove(Entity<MindComponent> ent, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        var jobSys = entMan.System<SharedJobSystem>();
        return !jobSys.CanBeKillTarget(ent);
    }
}
