using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent : EntityEventArgs
{
    public byte[] Data { get; }
    public NetEntity? SourceUid { get; }
    public bool IsWhisper { get; }
    public bool IsRadio { get; }

    public PlayTTSEvent(byte[] data, NetEntity? sourceUid = null, bool isWhisper = false, bool isRadio = false)
    {
        Data = data;
        SourceUid = sourceUid;
        IsWhisper = isWhisper;
        IsRadio = isRadio;
    }
}
