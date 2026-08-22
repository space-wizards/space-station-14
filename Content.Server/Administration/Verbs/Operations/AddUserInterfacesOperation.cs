namespace Content.Server.Administration.Verbs.Operations;

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

/// <summary>
/// Adds or replaces configured bound UIs without touching unrelated entries.
/// </summary>
public sealed partial class AddUserInterfacesOperation : AdminOperationBase<AddUserInterfacesOperation>
{
    [DataField(required: true)]
    public Dictionary<Enum, InterfaceData> Interfaces { get; private set; } = new();
}
