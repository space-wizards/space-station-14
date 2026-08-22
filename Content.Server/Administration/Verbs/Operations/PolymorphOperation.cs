using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnPolymorph(Entity<MetaDataComponent> entity, ref AdminOperationEvent<PolymorphOperation> args)
    {
        _polymorph.PolymorphEntity(entity, args.Operation.Prototype);
    }
}

// TODO: Use EntityEffectsOperation once the Polymorph effect supports targets without PolymorphableComponent.
public sealed partial class PolymorphOperation : AdminOperationBase<PolymorphOperation>
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype { get; private set; }
}
