using Content.Shared.Ghost;

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
    public GhostBooIntensity ResponseIntensity = GhostBooIntensity.None;
}
