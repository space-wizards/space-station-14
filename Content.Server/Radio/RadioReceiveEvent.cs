using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server.Radio;

[ByRefEvent]
public struct RadioReceiveEvent
{
    public readonly string Message;
    public readonly EntityUid MessageSource;
    public readonly RadioChannelPrototype Channel;
    public readonly MsgChatMessage ChatMsg;
    public readonly EntityUid RadioSource;

    public RadioReceiveEvent(string message, EntityUid messageSource, RadioChannelPrototype channel, MsgChatMessage chatMsg, EntityUid radioSource)
    {
        Message = message;
        MessageSource = messageSource;
        Channel = channel;
        ChatMsg = chatMsg;
        RadioSource = radioSource;
    }
}

[ByRefEvent]
public struct RadioReceiveAttemptEvent
{
    public readonly EntityUid MessageSource;
    public readonly RadioChannelPrototype Channel;
    public readonly EntityUid RadioSource;

    public bool Cancelled = false;

    public RadioReceiveAttemptEvent(EntityUid messageSource, RadioChannelPrototype channel, EntityUid radioSource)
    {
        MessageSource = messageSource;
        Channel = channel;
        RadioSource = radioSource;
    }
}
