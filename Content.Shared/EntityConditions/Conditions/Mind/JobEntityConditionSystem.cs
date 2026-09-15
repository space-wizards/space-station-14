using System.Linq;
using Content.Shared.Conditions;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

/// <summary>
/// Returns true if this entity has any of the specified jobs. False if the entity has no mind, none of the specified jobs, or is jobless.
/// </summary>
public sealed partial class MindContainerJobEntityConditionSystem : EntitySystem
{
    [Dependency] private SharedJobSystem _job = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MindContainerComponent> entity, ref ConditionEvaluationEvent<JobCondition> args)
    {
        args.Handled = true;

        args.Value = args.Condition.Jobs.Count(job=>_job.MindHasJobWithId(entity.Comp.Mind,job));
    }

    [SubscribeLocalEvent]
    private void Condition(Entity<MindComponent> entity, ref ConditionEvaluationEvent<JobCondition> args)
    {
        args.Handled = true;

        args.Value = (float)args.Condition.Jobs.Count(job=>_job.MindHasJobWithId(entity,job)) / (float)args.Condition.Jobs.Length;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class JobCondition : EntityConditionBase<JobCondition>
{
    /// <summary>
    /// Jobs required to fulfill this condition (only needs single match).
    /// </summary>
    [DataField(required: true)] public ProtoId<JobPrototype>[] Jobs = [];

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var localizedNames = Jobs.Select(jobId => prototype.Index(jobId).LocalizedName).ToList();
        return Loc.GetString("entity-condition-guidebook-job-condition", ("job", ContentLocalizationManager.FormatListToOr(localizedNames)));
    }
}
