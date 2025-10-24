using Content.Shared.Localizations;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if this entity have any of the listed tags.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class HasAnyTagEntityConditionSystem : EntityConditionSystem<TagComponent, AnyTagCondition>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void Condition(Entity<TagComponent> entity, ref EntityConditionEvent<AnyTagCondition> args)
    {
        args.Result = _tag.HasAnyTag(entity.Comp, args.Condition.Tags);
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class AnyTagCondition : EntityConditionBase<AnyTagCondition>
{
    [DataField(required: true)]
    public ProtoId<TagPrototype>[] Tags = [];

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var tagList = new List<string>();

        foreach (var type in Tags)
        {
            if (!prototype.Resolve(type, out var proto))
                continue;

            tagList.Add(proto.ID);
        }

        var names = ContentLocalizationManager.FormatListToOr(tagList);

        return Loc.GetString("entity-condition-guidebook-has-tag", ("tag", names), ("invert", Inverted));
    }
}
