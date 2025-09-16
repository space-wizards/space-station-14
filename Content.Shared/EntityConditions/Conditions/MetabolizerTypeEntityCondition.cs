using Content.Shared.Body.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

public sealed class MetabolizerType : EntityConditionBase<MetabolizerType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> Type = default!;
}
