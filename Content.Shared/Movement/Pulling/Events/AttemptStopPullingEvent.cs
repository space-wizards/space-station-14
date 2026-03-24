namespace Content.Shared.Pulling.Events;

/// <summary>
/// Raised when a request is made to stop pulling an entity.
/// </summary>

[ByRefEvent]
public record struct AttemptStopPullingEvent(EntityUid? User = null, bool Force = false)
{
    public readonly EntityUid? User = User;
    public readonly bool Force = Force;
    public bool Cancelled;
}
