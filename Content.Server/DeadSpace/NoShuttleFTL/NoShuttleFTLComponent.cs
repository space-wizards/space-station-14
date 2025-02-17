namespace Content.Server.DeadSpace.NoShuttleFTL;

[RegisterComponent]
public sealed partial class NoShuttleFTLComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool CantFTL = true;
}
