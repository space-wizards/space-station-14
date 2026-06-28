using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Projectile that does not start at full speed
/// </summary>
[RegisterComponent]
public sealed partial class AcceleratingProjectileComponent : Component
{
    [DataField]
    public float TargetSpeed = SharedGunSystem.ProjectileSpeed;

    [DataField]
    public float StartSpeed = SharedGunSystem.ProjectileSpeed * -0.15f;

    /// <summary>
    /// Time the projectile was fired
    /// </summary>
    [DataField]
    public TimeSpan FireTime;

    /// <summary>
    /// How long it takes for the projectile to go from <see cref="StartSpeed"/> to <see cref="TargetSpeed">
    /// </summary>
    [DataField]
    public TimeSpan FullAccelerationTime = TimeSpan.FromSeconds(0.6f);

    public float CurrentSpeed(TimeSpan currentTime)
    {
        return float.Lerp(StartSpeed, TargetSpeed,
            float.Clamp((float)((currentTime - FireTime) / FullAccelerationTime), 0.0f, 1.0f));
    }

    /// <summary>
    /// Ignoring prediction, should we remove the component?
    /// </summary>
    public bool DeletionTime(TimeSpan currentTime)
    {
        return currentTime > FireTime + FullAccelerationTime;
    }
}
