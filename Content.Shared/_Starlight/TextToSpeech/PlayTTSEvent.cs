using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.TextToSpeech;

[Serializable, NetSerializable]
public sealed class PlayTTSEvent : EntityEventArgs
{
    public byte[] Data { get; set; } = [];
    public NetEntity? SourceUid { get; set; }
    public bool IsRadio { get; set; }
    public float VolumeModifier { get; set; } = 1;

    public void SetVolumeModifier(float modifier)
    {
        VolumeModifier = Math.Clamp(modifier, 0, 3);
    }
}
