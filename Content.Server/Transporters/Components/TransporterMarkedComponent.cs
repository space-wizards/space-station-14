namespace Content.Server.Transporters.Components;

[RegisterComponent]
public sealed partial class TransporterMarkedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AssociatedProvider;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ClaimingTransporter;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Claimed;
}
