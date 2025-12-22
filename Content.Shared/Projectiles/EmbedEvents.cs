namespace Content.Shared.Projectiles;

/// <summary>
/// Raised directed on an entity when it embeds in another entity.
/// </summary>
[ByRefEvent]
public readonly record struct EmbedEvent(EntityUid? Shooter, EntityUid Embedded)
{
    /// <summary>
    /// The entity that threw/shot the embed, if any.
    /// </summary>
    public readonly EntityUid? Shooter = Shooter;

    /// <summary>
    /// Entity that is embedded in.
    /// </summary>
    public readonly EntityUid Embedded = Embedded;
}

/// <summary>
/// Raised directed on an entity when it stops being embedded in another entity.
/// </summary>
[ByRefEvent]
public readonly record struct EmbedDetachEvent(EntityUid? Detacher, EntityUid Embedded)
{
    /// <summary>
    /// The entity that detached the embed, if any.
    /// </summary>
    public readonly EntityUid? Detacher = Detacher;

    /// <summary>
    /// Entity that it is embedded in.
    /// </summary>
    public readonly EntityUid Embedded = Embedded;
}
