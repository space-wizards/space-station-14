using Robust.Shared.GameStates;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Map-placed component to automatically end the round after a delay spent in InRound.
/// Place this on the Map Entity in Resources/Maps to enable.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AutoRoundEndingComponent : Component
{
    /// <summary>
    /// Delay from entering InRound until triggering the end condition.
    /// </summary>
    [DataField("inRoundDelay")] public float InRoundDelay = 180f;

    /// <summary>
    /// Threshold (seconds remaining) to warn players before ending the round.
    /// </summary>
    [DataField("inRoundWarnThreshold")] public float InRoundWarnThreshold = 30f;

    /// <summary>
    /// Sender name for announcements.
    /// </summary>
    [DataField("senderName")] public string SenderName = "Мировая арена";

    /// <summary>
    /// Master enable switch.
    /// </summary>
    [DataField("enabled")] public bool Enabled = true;
}
