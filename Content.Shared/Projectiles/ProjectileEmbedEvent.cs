namespace Content.Shared.Projectiles;

/// <summary>
/// Raised directed on an entity when it embeds into something.
/// </summary>
[ByRefEvent]
public readonly record struct ProjectileEmbedEvent(EntityUid? Shooter, EntityUid? Weapon, EntityUid Embedded);
