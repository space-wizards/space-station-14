using Robust.Shared.Timing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
///     Will try to activate artifact periodically.
///     Doesn't used for random artifacts, can be spawned by admins.
/// </summary>
[RegisterComponent]
public class ArtifactTimerTriggerComponent : Component
{
    /// <summary>
    ///     Time between artifact activation attempts.
    /// </summary>
    [DataField("rate")]
    public TimeSpan ActivationRate = TimeSpan.FromSeconds(5.0f);

    /// <summary>
    ///     Last time when artifact was activated.
    /// </summary>
    public TimeSpan LastActivation;
}
