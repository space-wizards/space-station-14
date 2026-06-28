using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Projectile that does not start at full speed
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AcceleratingProjectileComponent : Component
{
    [DataField]
    public float TargetSpeed = SharedGunSystem.ProjectileSpeed;

    [DataField]
    public float StartSpeed = 0.0f;

    /// <summary>
    /// Time the projectile was fired
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan FireTime;

    /// <summary>
    /// How long it takes for the projectile to go from <see cref="StartSpeed"/> to <see cref="TargetSpeed">
    /// </summary>
    [DataField]
    public TimeSpan FullAccelerationTime = TimeSpan.FromSeconds(0.6f);

    /// <summary>
    /// Acceleration as metres per second.
    /// </summary>
    public float Acceleration => (TargetSpeed - StartSpeed) / (float)FullAccelerationTime.TotalSeconds;
}
