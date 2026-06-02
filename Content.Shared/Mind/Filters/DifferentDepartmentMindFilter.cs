using Content.Shared.Roles.Jobs;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind filter that requires minds to have a job with a different department from the excluded entity's.
/// This uses mind roles, not ID cards.
/// </summary>
public sealed partial class DifferentDepartmentMindFilter : MindFilter
{
    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan)
    {
        var jobSys = entMan.System<SharedJobSystem>();

        // no entity to exclude depts, so all depts are valid
        if (!exclude.HasValue)
            return false;

        if (!jobSys.MindTryGetJobId(exclude.Value, out var objJob))
            return false; // in no department, so all departments are valid

        if (!jobSys.MindTryGetJobId(mind.Owner, out var job))
            return false; // target in no department, so all depts are valid

        if (!objJob.HasValue || !job.HasValue)
            return false;

        // perform the department check
        if (jobSys.TryGetDepartment(objJob.Value, out var a) && jobSys.TryGetDepartment(job.Value, out var b))
            return a.ID == b.ID;

        return false;
    }
}
