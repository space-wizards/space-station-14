using Content.Shared.Conditions;
using Content.Shared.Localizations;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Conditions.UnifiedConditions;

namespace Content.Shared.EntityConditions.Conditions.Tags;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class AllTagsCondition : EntityConditionBase<IAllTagsCondition>, IAllTagsCondition
{
    /// <summary>
    /// Tags which all need to be possessed to fulfill the condition.
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

        var names = ContentLocalizationManager.FormatList(tagList);

        return Loc.GetString("entity-condition-guidebook-has-tag", ("tag", names), ("invert", Inverted));
    }

}
