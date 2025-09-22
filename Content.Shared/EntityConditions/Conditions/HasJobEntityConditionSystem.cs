using Content.Shared.EntityEffects;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Returns true if the entity has any of the specified jobs. False if the entity has no mind or none of the specified jobs, or is jobless.
/// </summary>
public sealed partial class HasJobEntityConditionSystem : EntityConditionSystem<MindContainerComponent, HasJob>
{
    protected override void Condition(Entity<MindContainerComponent> entity, ref EntityConditionEvent<HasJob> args)
    {
        // We need a mind in our mind container...
        if (!TryComp<MindComponent>(entity.Comp.Mind, out var mind))
            return;

        foreach (var roleId in mind.MindRoleContainer.ContainedEntities)
        {
            if (!HasComp<JobRoleComponent>(roleId))
                continue;

            if (!TryComp<MindRoleComponent>(roleId, out var mindRole))
            {
                Log.Error($"Encountered job mind role entity {roleId} without a {nameof(MindRoleComponent)}");
                continue;
            }

            if (mindRole.JobPrototype == null)
            {
                Log.Error($"Encountered job mind role entity {roleId} without a {nameof(JobPrototype)}");
                continue;
            }

            if (!args.Condition.Jobs.Contains(mindRole.JobPrototype.Value))
                continue;

            args.Result = true;
            return;
        }
    }
}

[DataDefinition]
public sealed partial class HasJob : EntityConditionBase<HasJob>
{
    [DataField(required: true)] public List<ProtoId<JobPrototype>> Jobs = [];
}
