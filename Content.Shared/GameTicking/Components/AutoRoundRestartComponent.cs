using Robust.Shared.GameStates;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Map-placed component to automatically restart the round after a delay spent in PostRound.
/// Place this on the Map Entity in Resources/Maps to enable.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AutoRoundRestartComponent : Component
{
    /// <summary>
    /// Delay from entering PostRound until triggering a restart.
    /// </summary>
    [DataField("postRoundDelay")] public float PostRoundDelay = 15f;

    /// <summary>
    /// Threshold (seconds remaining) to warn players before restarting the round.
    /// </summary>
    [DataField("postRoundWarnThreshold")] public float PostRoundWarnThreshold = 5f;

    /// <summary>
    /// Sender name for announcements.
    /// </summary>
    [DataField("senderName")] public string SenderName = "Мировая арена";

    /// <summary>
    /// Master enable switch.
    /// </summary>
    [DataField("enabled")] public bool Enabled = true;
}
