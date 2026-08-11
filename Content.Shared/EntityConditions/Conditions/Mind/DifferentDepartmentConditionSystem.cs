using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

public sealed partial class DifferentDepartmentConditionSystem : EntityConditionSystem<MindComponent, DifferentDepartmentCondition>
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;
    [Dependency] private SharedJobSystem _jobSystem = default!;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<DifferentDepartmentCondition> args)
    {
        args.Result = !IsInvalid(entity, args.SourceEnt);
    }

    private bool IsInvalid(Entity<MindComponent> mind, EntityUid? exclude)
    {
        // no entity to exclude depts, so all depts are valid
        if (!exclude.HasValue)
            return false;

        if (!_jobSystem.MindTryGetJobId(exclude.Value, out var objJob))
            return false; // in no department, so all departments are valid

        if (!_jobSystem.MindTryGetJobId(mind.Owner, out var job))
            return false; // target in no department, so all depts are valid

        if (!objJob.HasValue || !job.HasValue)
            throw new Exception("unreachable statement");

        // get all departments
        if (!_jobSystem.TryGetAllDepartments(objJob.Value, out var deptsA) || !_jobSystem.TryGetAllDepartments(job.Value, out var deptsB))
            throw new Exception("job didnt have any department");

        // perform the department check
        if (deptsA.Select(dept => dept.ID).Intersect(deptsB.Select(dept => dept.ID)).Any())
            return true;

        return false;
    }
}

/// <summary>
/// A condition that requires minds to have a job with a different department from the excluded entity's.
/// This uses mind roles, not ID cards.
/// </summary>
public sealed partial class DifferentDepartmentCondition : EntityConditionBase<DifferentDepartmentCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}

