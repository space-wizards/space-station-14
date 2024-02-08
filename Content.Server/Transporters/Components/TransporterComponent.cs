namespace Content.Server.Transporters;

[RegisterComponent]
public sealed partial class TransporterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;

    [ViewVariables(VVAccess.ReadWrite)]
    public float GrabRange = 1.5f;
}
