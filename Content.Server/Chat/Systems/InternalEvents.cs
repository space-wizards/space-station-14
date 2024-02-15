using Content.Shared.Chat.Prototypes;

namespace Content.Server.Chat.Systems;

public sealed class TransformSpeakerNameEvent : EntityEventArgs
{
    public EntityUid Sender;
    public string Name;
    public string? SpeechVerb;

    public TransformSpeakerNameEvent(EntityUid sender, string name, string? speechVerb = null)
    {
        Sender = sender;
        Name = name;
        SpeechVerb = speechVerb;
    }
}

/// <summary>
///     Raised broadcast in order to transform speech.transmit
/// </summary>
public sealed class TransformSpeechEvent : EntityEventArgs
{
    public EntityUid Sender;
    public string Message;

    public TransformSpeechEvent(EntityUid sender, string message)
    {
        Sender = sender;
        Message = message;
    }
}

public sealed class CheckIgnoreSpeechBlockerEvent : EntityEventArgs
{
    public EntityUid Sender;
    public bool IgnoreBlocker;

    public CheckIgnoreSpeechBlockerEvent(EntityUid sender, bool ignoreBlocker)
    {
        Sender = sender;
        IgnoreBlocker = ignoreBlocker;
    }
}

/// <summary>
///     InGame IC chat is for chat that is specifically ingame (not lobby) but is also in character, i.e. speaking.
/// </summary>
// ReSharper disable once InconsistentNaming
public enum InGameICChatType : byte
{
    Speak,
    Emote,
    Whisper
}
