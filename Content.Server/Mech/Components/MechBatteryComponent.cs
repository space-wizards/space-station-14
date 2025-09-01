namespace Content.Server.Mech.Components;

[RegisterComponent]
public sealed partial class MechBatteryComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid Mech;
}
