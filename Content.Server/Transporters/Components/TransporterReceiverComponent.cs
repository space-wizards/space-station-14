namespace Content.Server.Transporters.Components;

[RegisterComponent]
public sealed partial class TransporterReceiverComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Name;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid?> Providers;
}
