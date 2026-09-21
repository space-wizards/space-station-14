using Content.Shared.Conditions;
using Content.Shared.Localizations;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Conditions.UnifiedConditions;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class AnyTagCondition : EntityConditionBase<IHasAnyTagCondition>, IHasAnyTagCondition
{
    /// <summary>
    /// List of tags from which one must be matched.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype>[] Tags { get; set; } = [];

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
