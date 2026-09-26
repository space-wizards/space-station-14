using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Localizations;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class MetabolizerTypeCondition : EntityConditionBase<IMetabolizerTypeCondition>, IMetabolizerTypeCondition
{
    /// <summary>
    /// Which metabolizer types would fulfill this condition. Need only one match.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype>[] Type { get; set; } = default!;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var typeList = new List<string>();

        foreach (var type in Type)
        {
            if (!prototype.Resolve(type, out var proto))
                continue;

            typeList.Add(proto.LocalizedName);
        }

        var names = ContentLocalizationManager.FormatListToOr(typeList);

        return Loc.GetString("entity-condition-guidebook-organ-type",
            ("name", names),
            ("shouldhave", !Inverted));
    }
}
