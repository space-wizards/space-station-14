using Robust.Shared.GameStates;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Configuration component for the AutoRoundEnding game rule.
/// Allows tuning via prototype: time, sender, messages.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AutoRoundEndingRuleComponent : Component
{
    // Seconds from entering InRound until ending the round
    [DataField("inRoundDelay")] public float InRoundDelay = 180f;

    // Warn when remaining time is at or below this threshold (seconds)
    [DataField("inRoundWarnThreshold")] public float InRoundWarnThreshold = 30f;

    // Sender name for announcements
    [DataField("senderName")] public string SenderName = "Мировая арена";

    // Message shown when warning before ending
    [DataField("warnMessage")] public string WarnMessage =
        "Стороны не продвигаются в бою. До сосредоточенного авиаудара: {remaining} секунд.";

    // Message shown when ending triggers (optional; informational)
    [DataField("endMessage")] public string EndMessage = "Авиаудар нанесен. Бой окончен.";
}