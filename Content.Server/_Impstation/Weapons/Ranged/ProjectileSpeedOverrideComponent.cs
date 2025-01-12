namespace Content.Server._Impstation.Weapons.Ranged;

/// <summary>
/// Allows you to specify a projectile speed for an entity that is shot from a gun
/// </summary>
[RegisterComponent]
public sealed partial class ProjectileSpeedOverrideComponent : Component
{
    [DataField]
    public float SpeedOverride;
}
