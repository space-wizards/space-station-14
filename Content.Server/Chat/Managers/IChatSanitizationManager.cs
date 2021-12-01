using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Chat.Managers;

public interface IChatSanitizationManager
{
    public bool TrySanitizeOutSmilies(string input, out string sanitized, [NotNullWhen(true)] out string? emote);
}
