using Content.Shared.Eui;
using Robust.Shared.Serialization;
using System.Text;

namespace Content.Shared.Administration
{
    public static class AdminAnnounceDefaults
    {
        public const string DefaultColorHex = "#1d8bad";
        public const string ServerColorHex = "#f0973d";
        public const string DefaultSoundPath = "/Audio/Announcements/announce.ogg";

        public static string GetDefaultColorHex(AdminAnnounceType type)
        {
            return type == AdminAnnounceType.Server
                ? ServerColorHex
                : DefaultColorHex;
        }
    }

    public enum AdminAnnounceType
    {
        Station,
        Server,
    }

    [Serializable, NetSerializable]
    public sealed class AdminAnnounceEuiState : EuiStateBase;

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
        public static string NormalizeText(string? value) => value?.Trim() ?? string.Empty;

        public static string NormalizeSoundPath(string? value)
        {
            var path = NormalizeText(value);
            return IsValidResourcePath(path) ? path : string.Empty;
        }

        public static string GetValidatedColorHex(AdminAnnounceType type, string? value)
        {
            return TryNormalizeStrictHex(value, out var normalizedHex)
                ? normalizedHex
                : AdminAnnounceDefaults.GetDefaultColorHex(type);
        }

        public static bool TryNormalizeStrictHex(string? value, out string normalizedHex)
        {
            normalizedHex = string.Empty;
            var hex = NormalizeText(value);

            if (hex.Length != 7 || hex[0] != '#')
                return false;

            for (var i = 1; i < hex.Length; i++)
            {
                if (!Uri.IsHexDigit(hex[i]))
                    return false;
            }

            normalizedHex = hex;
            return true;
        }

        public static bool IsValidResourcePath(string? value)
        {
            var path = NormalizeText(value);
            return path.StartsWith('/') && !path.Contains("..") && !path.Contains('\\');
        }

        public static string FormatAnnouncement(string announcement, string? sender)
        {
            var trimmedSender = NormalizeText(sender);
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
