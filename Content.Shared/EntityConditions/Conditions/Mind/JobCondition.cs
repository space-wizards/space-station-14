using System.Linq;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class JobCondition : EntityConditionBase<IJobCondition>, IJobCondition
{
    /// <summary>
    /// Jobs required to fulfill this condition (only needs single match).
    /// </summary>
    [DataField(required: true)] public ProtoId<JobPrototype>[] Jobs { get; set; } = [];

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var localizedNames = Jobs.Select(jobId => prototype.Index(jobId).LocalizedName).ToList();
        return Loc.GetString("entity-condition-guidebook-job-condition", ("job", ContentLocalizationManager.FormatListToOr(localizedNames)));
    }
}
