using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IJobCondition : ICondition<IJobCondition>
{
    /// <summary>
    /// Jobs required to fulfill this condition (only needs single match).
    /// </summary>
    ProtoId<JobPrototype>[] Jobs { get; }
}

/// <summary>
/// Returns true if this entity has any of the specified jobs. False if the entity has no mind, none of the specified jobs, or is jobless.
/// </summary>
public sealed partial class JobConditionSystem : EntitySystem
{
    [Dependency] private SharedJobSystem _job = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindContainerComponent> entity, ref ConditionEvaluationEvent<IJobCondition> args)
    {
        args.Handled = true;

        args.Value = args.Condition.Jobs.Count(job=>_job.MindHasJobWithId(entity.Comp.Mind,job));
    }

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent<IJobCondition> args)
    {
        args.Handled = true;

        args.Value = (float)args.Condition.Jobs.Count(job=>_job.MindHasJobWithId(entity,job)) / (float)args.Condition.Jobs.Length;
    }
}
