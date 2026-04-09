using Content.Shared.Eui;
using Robust.Shared.Serialization;
using System.Text;

namespace Content.Shared.Administration
{
    public static class AdminAnnounceDefaults
    {
        public const string DefaultColorHex = "1d8bad";
        public const string ServerColorHex = "f0973d";
        public const string DefaultSoundPath = "/Audio/Announcements/announce.ogg";
    }

    public enum AdminAnnounceType
    {
        Station,
        Server,
    }

    [Serializable, NetSerializable]
    public sealed class AdminAnnounceEuiState : EuiStateBase { }

    public static class AdminAnnounceEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class DoAnnounce : EuiMessageBase
        {
            public bool CloseAfter;
            public string Announcer = default!;
            public string Announcement = default!;
            public AdminAnnounceType AnnounceType;
            public string ColorHex = AdminAnnounceDefaults.DefaultColorHex;
            public string SoundPath = AdminAnnounceDefaults.DefaultSoundPath;
            public string Sender = "";
        }
    }

    public static class AdminAnnounceHelpers
    {
        public static string CleanHex(string? hex) => hex?.Trim().TrimStart('#') ?? string.Empty;

        public static string FormatAnnouncement(string announcement, string? sender)
        {
            var trimmedSender = sender?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSender))
                return announcement;

            var sb = new StringBuilder(announcement);
            sb.Append('\n');
            sb.Append(Loc.GetString("admin-announce-sent-by"));
            sb.Append(' ');
            sb.Append(trimmedSender);
            return sb.ToString();
        }
    }
}
