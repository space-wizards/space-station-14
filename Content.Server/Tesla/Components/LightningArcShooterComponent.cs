
namespace Content.Server.Tesla.Components;
/// <summary>
/// Fires electric arcs at surrounding objects. Has a priority list of what to shoot at.
/// </summary>
[RegisterComponent]
public sealed partial class LightningArcShooterComponent : Component
{
    /// <summary>
    /// The number of lightning bolts that are fired at the same time. From 0 to N
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxLightningArc = 5;

    /// <summary>
    /// Minimum interval between shooting.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShootMinInterval = 0.5f;

    /// <summary>
    /// Maximum interval between shooting.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShootMaxInterval = 5.0f;

    /// <summary>
    /// the target selection radius for lightning bolts.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShootRange = 5f;

    /// <summary>
    /// How many times after a hit the lightning bolt will bounce into an adjacent target
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ArcDepth = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextShootTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string LightningPrototype = "Lightning";
}
