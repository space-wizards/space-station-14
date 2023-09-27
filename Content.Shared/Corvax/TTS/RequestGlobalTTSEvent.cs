using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.TTS;

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class RequestGlobalTTSEvent : EntityEventArgs
{
    public string Text { get; }
    public string VoiceId { get; }

    public RequestGlobalTTSEvent(string text, string voiceId)
    {
        Text = text;
        VoiceId = voiceId;
    }
}
