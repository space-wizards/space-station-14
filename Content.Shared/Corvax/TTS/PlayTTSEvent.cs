using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.TTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent : EntityEventArgs
{
    public EntityUid Uid { get; }
    public byte[] Data { get; }
    public bool IsRadio { get; }
    public float VolumeModifier { get; set; }

    public PlayTTSEvent(EntityUid uid, byte[] data, bool isRadio, float volumeModifier = 1f)
    {
        Uid = uid;
        Data = data;
        IsRadio = isRadio;
        VolumeModifier = volumeModifier;
    }

    public void SetVolumeModifier(float modifier)
    {
        VolumeModifier = Math.Clamp(modifier, 0, 3);
    }
}
