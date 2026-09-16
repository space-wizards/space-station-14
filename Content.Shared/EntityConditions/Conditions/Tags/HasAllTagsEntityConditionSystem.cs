using Content.Shared.Conditions;
using Content.Shared.Localizations;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Conditions.Interfaces;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if this entity has all the listed tags.
/// </summary>
public sealed partial class HasAllTagsEntityConditionSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<TagComponent> entity, ref ConditionEvaluationEvent<AllTagsCondition> args)
    {
        args.Handled = true;
        args.Value = args.Condition.Tags.Count(tag=>_tag.HasTag(entity.Comp, tag));
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class AllTagsCondition : EntityConditionBase<AllTagsCondition>, IWithThreshold
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

    IWithThreshold.Comparator IWithThreshold.Comparison => IWithThreshold.Comparator.Equal;

    float IWithThreshold.Threshold => Tags.Length;
}
