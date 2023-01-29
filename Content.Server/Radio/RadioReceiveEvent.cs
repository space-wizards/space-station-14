using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server.Radio;

public sealed class RadioReceiveEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;
    public readonly RadioChannelPrototype Channel;
    public readonly MsgChatMessage ChatMsg;

    public RadioReceiveEvent(string message, EntityUid source, RadioChannelPrototype channel, MsgChatMessage chatMsg)
    {
        Message = message;
        Source = source;
        Channel = channel;
        ChatMsg = chatMsg;
    }
}

public sealed class RadioReceiveAttemptEvent : CancellableEntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;
    public readonly RadioChannelPrototype Channel;

    public RadioReceiveAttemptEvent(string message, EntityUid source, RadioChannelPrototype channel)
    {
        Message = message;
        Source = source;
        Channel = channel;
    }
}
