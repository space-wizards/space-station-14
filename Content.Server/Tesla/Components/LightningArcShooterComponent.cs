
namespace Content.Server.Tesla.Components;
/// <summary>
/// Fires electric arcs at surrounding objects. Has a priority list of what to shoot at.
/// </summary>
[RegisterComponent]
public sealed partial class LightningArcShooterComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxLightningArc = 5;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShootMinInterval = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShootMaxInterval = 5.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShootRange = 5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ArcDepth = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextShootTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string LightningPrototype = "Lightning";
}
