using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnAddComponents(Entity<MetaDataComponent> entity, ref AdminOperationEvent<AddComponentsOperation> args)
    {
        EntityManager.AddComponents(entity, args.Operation.Components, args.Operation.ReplaceExisting);
    }
}

public sealed partial class AddComponentsOperation : AdminOperationBase<AddComponentsOperation>
{
    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();

    [DataField]
    public bool ReplaceExisting { get; private set; }
}
