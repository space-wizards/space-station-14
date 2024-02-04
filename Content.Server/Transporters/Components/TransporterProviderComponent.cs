namespace Content.Server.Transporters.Components;

[RegisterComponent]
public sealed partial class TransporterProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Name;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AssociatedReceiver;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> CurrentUnclaimedItems = new();

    public bool HasItems => CurrentUnclaimedItems.Count > 0;
}
