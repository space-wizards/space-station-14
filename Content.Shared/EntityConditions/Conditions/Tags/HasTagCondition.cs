using Content.Shared.Conditions;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class TagCondition : EntityConditionBase<IHasTagCondition>, IHasTagCondition
{
    /// <summary>
    /// Tag required to fulfill this condition.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag { get; set; }

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-has-tag", ("tag", Tag), ("invert", Inverted));
}
