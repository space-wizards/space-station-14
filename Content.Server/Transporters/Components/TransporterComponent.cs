namespace Content.Server.Transporters;

[RegisterComponent]
public sealed partial class TransporterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;
}
