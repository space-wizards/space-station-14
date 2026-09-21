using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IDifferentDepartmentCondition : ICondition<IDifferentDepartmentCondition>
{

}

public sealed partial class DifferentDepartmentConditionSystem : EntitySystem
{
    [Dependency] private SharedRoleSystem _roleSystem = default!;
    [Dependency] private SharedJobSystem _jobSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent<IDifferentDepartmentCondition> args)
    {
        args.Handled = true;
        args.Value = !IsInvalid(entity, args.SourceEntity) ? 1 : 0;
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
            throw new Exception("Unreachable statement after getting job proto from mind.");

        // get all departments
        if (!_jobSystem.TryGetAllDepartments(objJob.Value, out var deptsA) ||
            !_jobSystem.TryGetAllDepartments(job.Value, out var deptsB))
            throw new Exception("Job didn't have any department assigned.");

        // perform the department check
        if (deptsA.Select(dept => dept.ID).Intersect(deptsB.Select(dept => dept.ID)).Any())
            return true;

        return false;
    }
}
