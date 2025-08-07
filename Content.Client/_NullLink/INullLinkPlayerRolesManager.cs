
namespace Content.Client.Administration.Managers;

public interface INullLinkPlayerRolesManager
{
    event Action PlayerRolesChanged;

    bool ContainsAny(ulong[] roles);
    string? GetDiscordLink();
    void Initialize();
}