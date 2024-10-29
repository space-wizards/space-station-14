using Content.Shared.Chat;
using Content.Shared._EinsteinEngine.Language;
using Content.Shared.Radio;

namespace Content.Server.Radio;

/// <summary>
/// <param name="OriginalChatMsg">The message to display when the speaker can understand "language"</param>
/// <param name="LanguageObfuscatedChatMsg">The message to display when the speaker cannot understand "language"</param>
/// </summary>
[ByRefEvent]
public readonly record struct RadioReceiveEvent(
    // Einstein-Engines - languages mechanic
    EntityUid MessageSource,
    RadioChannelPrototype Channel,
    ChatMessage OriginalChatMsg,
    ChatMessage LanguageObfuscatedChatMsg,
    LanguagePrototype Language,
    EntityUid RadioSource

);
/// <summary>
/// Use this event to cancel sending message per receiver
/// </summary>
[ByRefEvent]
public record struct RadioReceiveAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource, EntityUid RadioReceiver)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public readonly EntityUid RadioReceiver = RadioReceiver;
    public bool Cancelled = false;
}

/// <summary>
/// Use this event to cancel sending message to every receiver
/// </summary>
[ByRefEvent]
public record struct RadioSendAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public bool Cancelled = false;
}
