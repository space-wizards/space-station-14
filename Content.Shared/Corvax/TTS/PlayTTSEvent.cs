using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent : EntityEventArgs
{
    public EntityUid Uid { get; }
    public byte[] Data { get; }

    public PlayTTSEvent(EntityUid uid, byte[] data)
    {
        Uid = uid;
        Data = data;
    }
}
