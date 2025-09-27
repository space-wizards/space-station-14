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
}
