using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server.Radio;

[ByRefEvent]
public readonly record struct RadioReceiveEvent(string Message, EntityUid MessageSource, RadioChannel Channel, MsgChatMessage ChatMsg);

/// <summary>
/// Use this event to cancel sending message per receiver
/// </summary>
[ByRefEvent]
public record struct RadioReceiveAttemptEvent(RadioChannel Channel, EntityUid RadioSource, EntityUid RadioReceiver)
{
    public readonly RadioChannel Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public readonly EntityUid RadioReceiver = RadioReceiver;
    public bool Cancelled = false;
}

/// <summary>
/// Use this event to cancel sending message to every receiver
/// </summary>
[ByRefEvent]
public record struct RadioSendAttemptEvent(RadioChannel Channel, EntityUid RadioSource)
{
    public readonly RadioChannel Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public bool Cancelled = false;
}
