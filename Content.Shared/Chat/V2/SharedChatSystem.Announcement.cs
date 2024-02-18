using System.Diagnostics.CodeAnalysis;
using Content.Shared.Communications;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

public partial class SharedChatSystem
{
    public static string SanitizeAnnouncement(string message, int maxLength = 0, int maxNewlines = 2)
    {
        var trimmed = message.Trim();
        if (maxLength > 0 && trimmed.Length > maxLength)
        {
            trimmed = $"{message[..maxLength]}...";
        }

        if (maxNewlines <= 0)
            return trimmed;

        var chars = trimmed.ToCharArray();
        var newlines = 0;
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] != '\n')
                continue;

            if (newlines >= maxNewlines)
                chars[i] = ' ';

            newlines++;
        }

        return new string(chars);
    }

    public bool SendCommunicationsConsoleAnnouncement(EntityUid console, EntityUid sender, string message, [NotNullWhen(true)] out string? reason)
    {
        if (!HasComp<SharedCommunicationsConsoleComponent>(console))
        {
            reason = Loc.GetString("chat-system-communication-console-announcement-failed");

            return false;
        }

        if (message.Length > MaxAnnouncementMessageLength)
        {
            reason = Loc.GetString("chat-system-max-message-length");

            return false;
        }

        RaiseNetworkEvent(new AttemptCommunicationConsoleAnnouncementMessage(GetNetEntity(console), GetNetEntity(sender), message));

        reason = "";

        return true;
    }
}

/// <summary>
/// Raised when an announcement is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class AnnouncementEvent : EntityEventArgs
{
    public string AsName;
    public readonly string Message;
    public Color? MessageColorOverride;

    public AnnouncementEvent( string asName, string message, Color? messageColorOverride = null)
    {
        AsName = asName;
        Message = message;
        MessageColorOverride = messageColorOverride;
    }
}
