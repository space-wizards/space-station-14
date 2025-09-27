using System.Linq;
using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// Checks if an entity has any of the given metabolizer types.
/// </summary>
public sealed partial class MetabolizerType : EntityConditionBase<MetabolizerType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype>[] Type = default!;

    // TODO: Convert to allow lists blah blah blah
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("reagent-effect-condition-guidebook-organ-type",
            ("name", prototype.Index(Type.FirstOrDefault()).LocalizedName),
            ("shouldhave", !Inverted));
}
