using System.Linq;
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
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class MindContainerJobEntityConditionSystem : EntityConditionSystem<MindContainerComponent, JobCondition>
{
    [Dependency] private SharedJobSystem _job = default!;

    protected override void Condition(Entity<MindContainerComponent> entity, ref EntityConditionEvent<JobCondition> args)
    {
        args.Result = _job.MindHasJobWithId(entity.Comp.Mind, args.Condition.Jobs);
    }
}

/// <summary>
/// Returns true if this mind has any of the specified jobs. False if the mind has none of the specified jobs, or is jobless.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class MindJobEntityConditionSystem : EntityConditionSystem<MindComponent, JobCondition>
{
    [Dependency] private SharedJobSystem _job = default!;

    protected override void Condition(Entity<MindComponent> entity, ref EntityConditionEvent<JobCondition> args)
    {
        args.Result = _job.MindHasJobWithId(entity, args.Condition.Jobs);
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
