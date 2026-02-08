namespace Content.Shared.Projectiles;

/// <summary>
/// Prevents projectiles from hitting this entity when the angle between the projectile's
/// direction of travel and the entity's facing direction exceeds a certain threshold
/// </summary>
[RegisterComponent]
public sealed partial class IgnoreProjectilesAboveAngleComponent : Component
{
    /// <summary>
    /// If this is true, the projectile will hit if it hits at an angle ABOVE the threshold,
    /// instead of below
    /// </summary>
    [DataField]
    public bool Reversed = false;

    /// <summary>
    /// If this is true, the threshold will be between the angle the projectile is
    /// travelling and the angle opposite from where the entity is facing
    /// </summary>
    [DataField]
    public bool Backwards = false;

    /// <summary>
    /// The maximum angle at which a projectile will hit
    /// </summary>
    [DataField]
    public Angle Angle = Angle.FromDegrees(90);

    /// <summary>
    /// If the shooter is farther away than this, the projectile will hit anyway.
    /// </summary>
    [DataField]
    public double MaximumDistance = 0.5;
}
