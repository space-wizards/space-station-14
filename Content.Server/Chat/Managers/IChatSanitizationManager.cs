using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Content.Server.Chat.Managers;

public interface IChatSanitizationManager
{
    public void Initialize();

    public bool TrySanitizeOutSmilies(string input, IEntity speaker, out string sanitized, [NotNullWhen(true)] out string? emote);
}
