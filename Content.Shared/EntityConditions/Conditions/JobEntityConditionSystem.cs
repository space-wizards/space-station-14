using System.Linq;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Returns true if this entity has any of the specified jobs. False if the entity has no mind, none of the specified jobs, or is jobless.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class HasJobEntityConditionSystem : EntityConditionSystem<MindContainerComponent, JobCondition>
{
    protected override void Condition(Entity<MindContainerComponent> entity, ref EntityConditionEvent<JobCondition> args)
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

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class JobCondition : EntityConditionBase<JobCondition>
{
    [DataField(required: true)] public List<ProtoId<JobPrototype>> Jobs = [];

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var localizedNames = Jobs.Select(jobId => prototype.Index(jobId).LocalizedName).ToList();
        return Loc.GetString("entity-condition-guidebook-job-condition", ("job", ContentLocalizationManager.FormatListToOr(localizedNames)));
    }
}
