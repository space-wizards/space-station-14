using Content.Shared.Conditions;
using Content.Shared.Localizations;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <summary>
/// Returns true if this entity have any of the listed tags.
/// </summary>
public sealed partial class HasAnyTagEntityConditionSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;

    private void Condition(Entity<TagComponent> entity, ref ConditionEvaluationEvent<AnyTagCondition> args)
    {
        args.Handled = true;
        //count matches to scale, as default condition if value != 0 -> satisfy == true.
        args.Value = args.Condition.Tags.Count(tag => _tag.HasTag(entity.Comp, tag)) /
                     (float)args.Condition.Tags.Length;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class AnyTagCondition : EntityConditionBase<AnyTagCondition>
{
    /// <summary>
    /// List of tags from which one must be matched.
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

        var names = ContentLocalizationManager.FormatListToOr(tagList);

        return Loc.GetString("entity-condition-guidebook-has-tag", ("tag", names), ("invert", Inverted));
    }
}
