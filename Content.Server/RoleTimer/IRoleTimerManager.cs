using System.Threading.Tasks;
using Robust.Shared.Network;

namespace Content.Server.RoleTimer;

public interface IRoleTimerManager
{
    Task SetPlaytimeForRole(NetUserId player, string role, TimeSpan time);
    Task<TimeSpan?> GetPlaytimeForRole(NetUserId player, string role);
    Task<Dictionary<string, TimeSpan>?> GetPlaytimeAllRoles(NetUserId player);
}
