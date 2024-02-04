namespace Content.Server.Transporters.Components;

[RegisterComponent]
public sealed partial class MarkedForTransportComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? AssociatedProvider;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ClaimingTransporter;

    public bool Claimed => ClaimingTransporter is not null;
}
