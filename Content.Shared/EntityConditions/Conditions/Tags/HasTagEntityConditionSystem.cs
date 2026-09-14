using Content.Shared.Conditions;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if this entity has the listed tag.
/// </summary>
public sealed partial class HasTagEntityConditionSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TagComponent> entity, ref ConditionEvaluationEvent args)
    {
        if (args.Handled || args.Condition is not TagCondition condition)
            return;
        args.Handled = true;

        args.Value = _tag.HasTag(entity.Comp, condition.Tag) ? 1 : 0;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class TagCondition : EntityConditionBase<TagCondition>
{
    /// <summary>
    /// Tag required to fulfill this condition.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-has-tag", ("tag", Tag), ("invert", Inverted));
}
