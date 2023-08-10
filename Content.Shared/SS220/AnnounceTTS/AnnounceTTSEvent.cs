// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
