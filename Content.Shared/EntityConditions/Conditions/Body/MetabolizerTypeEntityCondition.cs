using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Body;

public sealed partial class MetabolizerType : EntityConditionBase<MetabolizerType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> Type;
}
