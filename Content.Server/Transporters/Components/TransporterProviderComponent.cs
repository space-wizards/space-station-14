namespace Content.Server.Transporters.Components;

[RegisterComponent]
public sealed partial class TransporterProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Name;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AssociatedReceiver;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> CurrentItems = new();

    public bool HasItems => CurrentItems.Count > 0;
}
