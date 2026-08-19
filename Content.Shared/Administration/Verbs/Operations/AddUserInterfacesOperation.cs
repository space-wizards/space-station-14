namespace Content.Shared.Administration.Verbs.Operations;

/// <summary>
/// Adds or replaces configured bound UIs without touching unrelated entries.
/// </summary>
public sealed partial class AddUserInterfacesOperation : AdminOperationBase<AddUserInterfacesOperation>
{
    [DataField(required: true)]
    public Dictionary<Enum, InterfaceData> Interfaces { get; private set; } = new();
}
