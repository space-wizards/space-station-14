using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Starlight.NullLink.Event;

namespace Content.Server._NullLink.PlayerData;
public interface INullLinkPlayerManager
{
    void Shutdown();
    ValueTask SyncRoles(PlayerRolesSyncEvent ev);
    bool TryGetPlayerData(Guid userId, [NotNullWhen(true)] out PlayerData? playerData);
    ValueTask UpdateRoles(RolesChangedEvent ev);
}