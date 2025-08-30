namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised to get the effective shooter for projectiles.
/// </summary>
[ByRefEvent]
public struct GetProjectileShooterEvent
{
    public EntityUid? ProjectileShooter;

    public bool Handled;
}
