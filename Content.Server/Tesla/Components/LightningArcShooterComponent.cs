using Content.Server.Tesla.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Tesla.Components;

/// <summary>
/// Periodically fires electric arcs at surrounding objects.
/// </summary>
[RegisterComponent, Access(typeof(LightningArcShooterSystem))]
public sealed partial class LightningArcShooterComponent : Component
{
    /// <summary>
    /// The number of lightning bolts that are fired at the same time. From 0 to N
    /// Important balance value: if there aren't a N number of coils or grounders around the tesla,
    /// the tesla will have a chance to shoot into something important and break.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxLightningArc = 1;

    /// <summary>
    /// Minimum interval between shooting.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShootMinInterval = 0.5f;

    /// <summary>
    /// Maximum interval between shooting.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ShootMaxInterval = 8.0f;

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

    /// <summary>
    /// The time, upon reaching which the next batch of lightning bolts will be fired.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextShootTime;

    /// <summary>
    /// The type of lightning bolts we shoot
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId LightningPrototype = "Lightning";
}
