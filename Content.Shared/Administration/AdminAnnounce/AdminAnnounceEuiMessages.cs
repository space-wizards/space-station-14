using Content.Shared.Eui;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.AdminAnnounce;

public enum AdminAnnounceType
{
    Station,
    Server
}

public static class AdminAnnounceEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class DoAnnounce(
        string announcement,
        string announcer,
        string signature,
        Color color,
        SoundPathSpecifier? sound,
        AdminAnnounceType announceType,
        MapId? mapId,
        bool closeAfter)
        : EuiMessageBase
    {
        public string Announcement { get; } = announcement;
        public string Announcer { get; } = announcer;
        public string Signature { get; } = signature;
        public Color Color { get; } = color;
        public SoundPathSpecifier? Sound { get; } = sound;
        public AdminAnnounceType AnnounceType { get; } = announceType;
        public MapId? MapId { get; } = mapId;
        public bool CloseAfter { get; } = closeAfter;
    }
}
