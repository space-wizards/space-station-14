using Content.Shared.Conditions.UnifiedConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class InternalsCondition : EntityConditionBase<IInternalsCondition>, IInternalsCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-internals", ("usingInternals", !Inverted));
}
