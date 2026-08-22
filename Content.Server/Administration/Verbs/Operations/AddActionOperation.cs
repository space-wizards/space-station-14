using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnAddAction(Entity<MetaDataComponent> entity, ref AdminOperationEvent<AddActionOperation> args)
    {
        foreach (var action in _actions.GetActions(entity))
        {
            if (args.Operation.Action.Equals(MetaData(action).EntityPrototype?.ID))
                return;
        }

        _actions.AddAction(entity, args.Operation.Action);
    }
}

/// <summary>
/// Adds an action unless the target already has one from the same prototype.
/// </summary>
public sealed partial class AddActionOperation : AdminOperationBase<AddActionOperation>
{
    [DataField(required: true)]
    public EntProtoId Action { get; private set; }
}
