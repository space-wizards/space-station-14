using Content.Shared.Localizations;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if this entity has all the listed tags.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class HasAllTagsEntityConditionSystem : EntityConditionSystem<TagComponent, AllTagsCondition>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void Condition(Entity<TagComponent> entity, ref EntityConditionEvent<AllTagsCondition> args)
    {
        args.Result = _tag.HasAllTags(entity.Comp, args.Condition.Tags);
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class AllTagsCondition : EntityConditionBase<AllTagsCondition>
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

        var names = ContentLocalizationManager.FormatList(tagList);

        return Loc.GetString("entity-condition-guidebook-has-tag", ("tag", names), ("invert", Inverted));
    }
}
