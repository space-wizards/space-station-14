using Content.Shared.Ghost;

namespace Content.Server.Ghost;

/// <summary>
/// An event raised by ghosts to cause spooky events (e.g. flickering lights) in the vicinity.
/// </summary>
public sealed class GhostBooEvent(GhostBooIntensity allowedIntensity) : EntityEventArgs
{
    /// <summary>
    /// The type of action that was performed, if any.
    /// Should only be set if something happened during the action.
    /// </summary>
    public GhostBooIntensity ResponseIntensity = GhostBooIntensity.None;

    /// <summary>
    /// The level of intensity that the action allows.
    /// </summary>
    public readonly GhostBooIntensity AllowedIntensity = allowedIntensity;
}
