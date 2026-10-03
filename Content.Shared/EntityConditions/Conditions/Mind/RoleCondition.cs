using System.Linq;
using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Mind;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class RoleCondition : EntityConditionBase<IRoleCondition>, IRoleCondition
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist { get; set; } = new();

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}
