namespace Content.Server.Atmos.Components;

/// <summary>
/// This entity will try to toggle their internals at the specified time
/// </summary>
[RegisterComponent]
public sealed partial class InternalsDelayedActivationComponent : Component
{
    /// <summary>
    /// The entity that will toggle internals
    /// </summary>
    public EntityUid Entity = new EntityUid();

    /// <summary>
    /// The server time when the internals will be toggled
    /// </summary>
    public TimeSpan Time = new TimeSpan();
}

