namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Raised when an entity that was thrown lands. This occurs before they stop moving and is when their tile-friction is reapplied.
    /// </summary>
    [ByRefEvent]
    public readonly record struct LandEvent(EntityUid? User, bool PlaySound);

    /// <summary>
    /// Raised when a thrown entity is no longer moving.
    /// </summary>
    [ByRefEvent]
    public record struct StopThrowEvent(EntityUid? User);
}
