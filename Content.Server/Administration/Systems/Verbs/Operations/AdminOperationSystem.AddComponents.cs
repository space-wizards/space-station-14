using Content.Server.Administration.Verbs.Operations;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnAddComponents(Entity<MetaDataComponent> entity, ref AdminOperationEvent<AddComponentsOperation> args)
    {
        EntityManager.AddComponents(entity, args.Operation.Components, args.Operation.ReplaceExisting);
    }
}
