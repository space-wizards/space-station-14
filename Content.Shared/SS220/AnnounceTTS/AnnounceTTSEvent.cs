using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AnnounceTTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class AnnounceTTSEvent : EntityEventArgs
{
    public AnnounceTTSEvent(byte[] data, string announcementSound, AudioParams announcementParams)
    {
        Data = data;
        AnnouncementSound = announcementSound;
        AnnouncementParams = announcementParams;
    }
    public byte[] Data { get; }
    public string AnnouncementSound { get; }
    public AudioParams AnnouncementParams{ get; }
}
