using Content.Server.Administration.Verbs.Operations;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnAddUserInterfaces(Entity<MetaDataComponent> entity, ref AdminOperationEvent<AddUserInterfacesOperation> args)
    {
        var userInterface = EnsureComp<UserInterfaceComponent>(entity);

        foreach (var (key, data) in args.Operation.Interfaces)
        {
            _ui.SetUi((entity.Owner, userInterface), key, data);
        }
    }
}
