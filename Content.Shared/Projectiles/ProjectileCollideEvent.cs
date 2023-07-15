namespace Content.Shared.Projectiles;

/// <summary>
/// Raised directed on what a projectile collides with. Can have its deletion cancelled.
/// </summary>
[ByRefEvent]
public record struct ProjectileCollideEvent(EntityUid OtherEntity, bool Cancelled);
