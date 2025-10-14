using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Shared game rule config for automatic post-round restart.
/// Systems reference this to pull delays and messages.
/// </summary>
[RegisterComponent]
public sealed partial class AutoRoundRestartRuleComponent : Component
{
    /// <summary>
    /// Seconds to wait in PostRound before restarting.
    /// </summary>
    [DataField] public float PostRoundDelay = 15f;

    /// <summary>
    /// Threshold in seconds to issue a warning before restart.
    /// </summary>
    [DataField] public float PostRoundWarnThreshold = 5f;

    // Optional: list of multiple thresholds to announce (seconds). If provided, supersedes PostRoundWarnThreshold.
    [DataField("warnThresholds")] public List<float> WarnThresholds = new();

    /// <summary>
    /// Name used in announcements.
    /// </summary>
    [DataField] public string SenderName = ""; // prototype must set

    /// <summary>
    /// Warning message template. Use {remaining} placeholder for seconds.
    /// </summary>
    [DataField] public string WarnMessage = ""; // prototype must set

    // Optional: list of messages mapped 1:1 to warnThresholds. If count mismatches, WarnMessage is used.
    [DataField("warnMessages")] public List<string>? WarnMessages;

    /// <summary>
    /// Message announced right before restarting.
    /// </summary>
    [DataField] public string RestartMessage = ""; // prototype must set
}
