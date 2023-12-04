namespace Content.Server.Sticky.Events;

/// <summary>
///     Risen on sticky entity to see if it can stick to another entity.
/// </summary>
[ByRefEvent]
public record struct AttemptEntityStickEvent(EntityUid Target, EntityUid User)
{
    public readonly EntityUid Target = Target;
    public readonly EntityUid User = User;
    public bool Cancelled = false;
}

/// <summary>
///     Risen on sticky entity to see if it can unstick from another entity.
/// </summary>
[ByRefEvent]
public record struct AttemptEntityUnstickEvent(EntityUid Target, EntityUid User)
{
    public readonly EntityUid Target = Target;
    public readonly EntityUid User = User;
    public bool Cancelled = false;
}


/// <summary>
///     Risen on sticky entity when it was stuck to other entity.
/// </summary>
public sealed class EntityStuckEvent : EntityEventArgs
{
    /// <summary>
    ///     Entity that was used as a surface for sticky object.
    /// </summary>
    public readonly EntityUid Target;

    /// <summary>
    ///     Entity that stuck sticky object on target.
    /// </summary>
    public readonly EntityUid User;

    public EntityStuckEvent(EntityUid target, EntityUid user)
    {
        Target = target;
        User = user;
    }
}

/// <summary>
///     Risen on sticky entity when it was unstuck from other entity.
/// </summary>
public sealed class EntityUnstuckEvent : EntityEventArgs
{
    /// <summary>
    ///     Entity that was used as a surface for sticky object.
    /// </summary>
    public readonly EntityUid Target;

    /// <summary>
    ///     Entity that unstuck sticky object on target.
    /// </summary>
    public readonly EntityUid User;

    public EntityUnstuckEvent(EntityUid target, EntityUid user)
    {
        Target = target;
        User = user;
    }
}
