using Content.Shared.Conditions;
using Content.Shared.Localizations;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if this entity has all the listed tags.
/// </summary>
public sealed partial class HasAllTagsEntityConditionSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TagComponent> entity, ref ConditionEvaluationEvent args)
    {
        if (args.Handled || args.Condition is not AllTagsCondition condition)
            return;
        args.Handled = true;
        args.Value = condition.Tags.Count(tag=>_tag.HasTag(entity.Comp, tag));
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class AllTagsCondition : EntityConditionBase<AllTagsCondition>, IConditionWithThreshold
{
    /// <summary>
    /// Tags which all need to be possessed to fulfill the condition.
    /// </summary>
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

    IConditionWithThreshold.Comparator IConditionWithThreshold.Comparison => IConditionWithThreshold.Comparator.Equal;

    float IConditionWithThreshold.Threshold => Tags.Length;
}
