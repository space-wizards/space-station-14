namespace Content.Shared.Revolutionary;

/// <summary>
/// Raised when a revolutionary conversion is being attempted on an entity.
/// </summary>
[ByRefEvent]
public record struct AttemptConvertRevolutionaryEvent(bool Cancelled);
