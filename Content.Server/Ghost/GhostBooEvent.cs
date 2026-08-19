using Content.Shared.Ghost.Components;

namespace Content.Server.Ghost;

/// <summary>
/// A targeted event raised to cause spooky events (e.g. flickering lights).
/// </summary>
/// <remarks>
/// Most frequently used by ghosts to haunt an area, hence the name.
/// </remarks>
[ByRefEvent]
public struct GhostBooEvent(GhostBooIntensity allowedIntensity)
{
    /// <summary>
    /// The maximum level of intensity that the caller will allow.
    /// </summary>
    public readonly GhostBooIntensity AllowedIntensity = allowedIntensity;

    /// <summary>
    /// The type of action that was performed, if any.
    /// Should only be set by handling entities if something happened.
    /// </summary>
    public GhostBooIntensity ResponseIntensity { get; private set; } = GhostBooIntensity.None;

    /// <summary>
    /// Whether or not the event has been handled already.
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Sets both the handled status of this event and the intensity of its response.
    /// </summary>
    public void SetResponseIntensity(GhostBooIntensity intensity)
    {
        Handled = true;
        ResponseIntensity = intensity;
    }
}
