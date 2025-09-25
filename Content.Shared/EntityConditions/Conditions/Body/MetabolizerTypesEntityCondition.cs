using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

/// <summary>
/// Like <see cref="MetabolizerType"/> but checks multiple types!
/// </summary>
public sealed partial class MetabolizerTypes : EntityConditionBase<MetabolizerTypes>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype>[] Types = default!;
}
