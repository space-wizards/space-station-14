using Content.Server.Administration.Verbs.Operations;

namespace Content.Server.Administration.Systems.Verbs.Operations;

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
