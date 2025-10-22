using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.Languages.Prototypes;

namespace Content.Server.Radio;

[ByRefEvent]
public readonly record struct RadioReceiveEvent(string Message, ProtoId<LanguagePrototype> LanguageId, EntityUid MessageSource, RadioChannelPrototype Channel, EntityUid RadioSource, MsgChatMessage ChatMsg, MsgChatMessage LexiconChatMsg, List<EntityUid> Receivers);

/// <summary>
/// Event raised on the parent entity of a headset radio when a radio message is received
/// </summary>
[ByRefEvent]
public readonly record struct HeadsetRadioReceiveRelayEvent(RadioReceiveEvent RelayedEvent);

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
