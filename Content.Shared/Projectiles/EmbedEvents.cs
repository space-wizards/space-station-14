namespace Content.Shared.Projectiles;

/// <summary>
/// Raised directed on an entity when it embeds in another entity.
/// </summary>
[ByRefEvent]
public readonly record struct EmbedEvent(EntityUid? Shooter, EntityUid Embedded)
{
    public readonly EntityUid? Shooter = Shooter;

    /// <summary>
    /// Entity that is embedded in.
    /// </summary>
    public readonly EntityUid Embedded = Embedded;
}

/// <summary>
/// Raised directed on an entity when it stops embedding in another entity.
/// </summary>
[ByRefEvent]
public readonly record struct StopEmbedEvent(EntityUid? Detacher, EntityUid Embedded)
{
    public readonly EntityUid? Detacher = Detacher;

    /// <summary>
    /// Entity that it is embedded in.
    /// </summary>
    public readonly EntityUid Embedded = Embedded;
}
