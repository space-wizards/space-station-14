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
/// Imp edit, raised on an entity when another entity is embedded into it.
/// </summary>
[ByRefEvent]
public readonly record struct EmbeddedEvent(EntityUid? Shooter, EntityUid Embedded)
{
    public readonly EntityUid? Shooter = Shooter;

    /// <summary>
    /// Entity that is embedded into this.
    /// </summary>
    public readonly EntityUid Embedded = Embedded;
}

/// <summary>
/// Raised on an entity when it stops embedding in another entity.
/// </summary>
[ByRefEvent]
public readonly record struct RemoveEmbedEvent(EntityUid? Remover)
{
    public readonly EntityUid? Remover = Remover;
}
