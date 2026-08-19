using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations;

// TODO: Use EntityEffectsOperation once the Polymorph effect supports targets without PolymorphableComponent.
public sealed partial class PolymorphOperation : AdminOperationBase<PolymorphOperation>
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype { get; private set; }
}
