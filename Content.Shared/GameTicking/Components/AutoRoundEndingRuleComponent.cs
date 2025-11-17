using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Configuration component for the AutoRoundEnding game rule.
/// Allows tuning via prototype: time, sender, messages.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AutoRoundEndingRuleComponent : Component, ISerializationHooks
{
    // Seconds from entering InRound until ending the round
    [DataField("inRoundDelay")] public float InRoundDelay = 600f;

    // Optional alias to configure the same delay via 'roundDelay' in YAML.
    // If set (> 0), it overrides InRoundDelay after deserialization.
    [DataField("roundDelay")] public float? RoundDelay { get; set; }

    // Warn when remaining time is at or below this threshold (seconds)
    [DataField("inRoundWarnThreshold")] public float InRoundWarnThreshold = 30f;

    // Optional: list of multiple thresholds to announce (seconds). If provided, supersedes inRoundWarnThreshold.
    [DataField("warnThresholds")] public List<float> WarnThresholds = new();

    // Sender name for announcements
    [DataField("senderName")] public string SenderName = "Мировая арена";

    // Message shown when warning before ending
    [DataField("warnMessage")] public string WarnMessage =
        "Стороны не продвигаются в бою. До сосредоточенного авиаудара: {remaining} секунд.";

    // Optional: list of messages mapped 1:1 to warnThresholds. If count mismatches, WarnMessage is used.
    [DataField("warnMessages")] public List<string>? WarnMessages;

    // Message shown when ending triggers (optional; informational)
    [DataField("endMessage")] public string EndMessage = "Авиаудар нанесен. Бой окончен.";

    // Optional message to announce immediately when the round starts (InRound)
    // Leave empty to disable.
    [DataField("startMessage")] public string StartMessage = string.Empty;

    // Center HUD: optional label shown near the countdown timer
    [DataField("hudLabel")] public string HudLabel = string.Empty;

    // Center HUD: RSI path relative to /Textures (e.g. "Objects/counterstrike/Other/interface.rsi")
    [DataField("hudIconRsi")] public string? HudIconRsi;

    // Center HUD: RSI state name within the RSI file (e.g. "cs")
    [DataField("hudIconState")] public string? HudIconState;

    void ISerializationHooks.AfterDeserialization()
    {
        if (RoundDelay is { } d && d > 0f)
            InRoundDelay = d;
    }
}