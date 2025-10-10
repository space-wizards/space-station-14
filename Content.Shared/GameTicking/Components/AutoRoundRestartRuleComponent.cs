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

    /// <summary>
    /// Name used in announcements.
    /// </summary>
    [DataField] public string SenderName = "Мировая арена";

    /// <summary>
    /// Warning message template. Use {remaining} placeholder for seconds.
    /// </summary>
    [DataField] public string WarnMessage = "Авиаудар нанесен. Конец боя через: {remaining} секунд!";

    /// <summary>
    /// Message announced right before restarting.
    /// </summary>
    [DataField] public string RestartMessage = "Новый раунд начинается!";
}
