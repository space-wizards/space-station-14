using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.TextToSpeech;

[Serializable, NetSerializable]
public sealed class ClientOptionTTSEvent : EntityEventArgs
{
    public bool Enabled { get; set; }
}
