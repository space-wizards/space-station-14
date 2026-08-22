using Content.Server.Administration.Verbs.Operations;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnPolymorph(Entity<MetaDataComponent> entity, ref AdminOperationEvent<PolymorphOperation> args)
    {
        _polymorph.PolymorphEntity(entity, args.Operation.Prototype);
    }
}
