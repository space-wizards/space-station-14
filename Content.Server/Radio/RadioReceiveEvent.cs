using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.Map;

namespace Content.Server.Radio;

[ByRefEvent]
public struct RadioReceiveEvent
{
    public readonly string Message;
    public readonly EntityUid MessageSource;
    public readonly RadioChannelPrototype Channel;
    public readonly MsgChatMessage ChatMsg;

    public RadioReceiveEvent(string message, EntityUid messageSource, RadioChannelPrototype channel, MsgChatMessage chatMsg)
    {
        Message = message;
        MessageSource = messageSource;
        Channel = channel;
        ChatMsg = chatMsg;
    }
}

/// <summary>
/// Use this event to cancel sending messages by doing various checks (e.g. range)
/// </summary>
[ByRefEvent]
public struct RadioReceiveAttemptEvent
{
    public readonly RadioChannelPrototype Channel;
    public readonly EntityUid RadioSource;
    public readonly EntityUid RadioReceiver;
    public readonly MapId MapId;

    public bool Cancelled = false;

    public RadioReceiveAttemptEvent(RadioChannelPrototype channel, EntityUid radioSource, EntityUid radioReceiver, MapId mapId)
    {
        Channel = channel;
        RadioSource = radioSource;
        RadioReceiver = radioReceiver;
        MapId = mapId;
    }
}
