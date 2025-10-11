using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if this entity has the listed tag.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class HasTagEntityConditionSystem : EntityConditionSystem<TagComponent, HasTag>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void Condition(Entity<TagComponent> entity, ref EntityConditionEvent<HasTag> args)
    {
        args.Result = _tag.HasTag(entity.Comp, args.Condition.Tag);
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class HasTag : EntityConditionBase<HasTag>
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("reagent-effect-condition-guidebook-has-tag", ("tag", Tag), ("invert", Inverted));
}
