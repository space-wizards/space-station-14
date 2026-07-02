using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if this entity has the listed tag.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class HasTagEntityConditionSystem : EntityConditionSystem<TagComponent, TagCondition>
{
    [Dependency] private TagSystem _tag = default!;

    protected override void Condition(Entity<TagComponent> entity,
        TagCondition condition,
        EntityUid? sourceEnt,
        ref bool result)
    {
        result = _tag.HasTag(entity.Comp, condition.Tag);
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class TagCondition : EntityCondition
{
    /// <summary>
    /// Tag required to fulfill this condition.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-has-tag", ("tag", Tag), ("invert", Inverted));
}
