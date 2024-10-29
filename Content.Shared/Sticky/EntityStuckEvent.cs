namespace Content.Shared.Sticky;

/// <summary>
///     Risen on sticky entity to see if it can stick to another entity.
/// </summary>
[ByRefEvent]
public record struct AttemptEntityStickEvent(EntityUid Target, EntityUid User, bool Cancelled = false);

/// <summary>
///     Risen on sticky entity to see if it can unstick from another entity.
/// </summary>
[ByRefEvent]
public record struct AttemptEntityUnstickEvent(EntityUid Target, EntityUid User, bool Cancelled = false);


/// <summary>
///     Risen on sticky entity when it was stuck to other entity.
/// </summary>
[ByRefEvent]
public record struct EntityStuckEvent(EntityUid Target, EntityUid User);

/// <summary>
///     Risen on sticky entity when it was unstuck from other entity.
/// </summary>
[ByRefEvent]
public record struct EntityUnstuckEvent(EntityUid Target, EntityUid User);
