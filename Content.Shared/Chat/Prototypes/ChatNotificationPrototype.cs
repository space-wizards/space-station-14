using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.Prototypes;

/// <summary>
/// A predefined notification used to warn a player of specific events
/// </summary>
[Prototype("chatNotification")]
public sealed partial class ChatNotificationPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The notification that the player receives
    /// </summary>
    /// <remarks>
    /// Use '{$source}', '{user}', and '{target}' in the fluent message
    /// to insert the source, user, and target names respectively
    /// </remarks>
    [DataField(required: true)]
    public LocId Message = string.Empty;

    /// <summary>
    /// Font color for the notification
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Sound played upon receiving the notification
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Amount of time that must elapse before the next notification
    /// </summary>
    [DataField]
    public float NextDelay = 10f;

    /// <summary>
    /// Determines whether notification delays should be determined by the source
    /// entity or by the notification prototype (i.e., individual notifications
    /// vs grouping the notifications together)
    /// </summary>
    [DataField]
    public bool NotifyBySource = false;
}

[ByRefEvent]
public record ChatNotificationEvent(ProtoId<ChatNotificationPrototype> ChatNotification, EntityUid Source, EntityUid? User = null)
{
    public string? SourceNameOverride;
    public string? UserNameOverride;
}
