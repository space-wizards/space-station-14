using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IJobCondition : ICondition
{
    /// <summary>
    /// Jobs required to fulfill this condition (only needs single match).
    /// </summary>
    ProtoId<JobPrototype>[] Jobs { get; }
}

/// <summary>
/// Returns true if this entity has any of the specified jobs. False if the entity has no mind, none of the specified jobs,
/// or is jobless.
/// </summary>
public sealed partial class JobConditionSystem : ConditionEvaluatorSystem<IJobCondition>
{
    [Dependency] private SharedJobSystem _job = default!;

    public override float Evaluate(IJobCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return condition.Jobs.Count(job => _job.MindHasJobWithId(entityUid, job)) /  (float)condition.Jobs.Length;
    }
}
