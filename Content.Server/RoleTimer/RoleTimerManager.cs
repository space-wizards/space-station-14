using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;

namespace Content.Server.RoleTimer
{
    public sealed class RoleTimerManager : IRoleTimerManager
    {
        [Dependency] private readonly IServerDbManager _db = default!;

        public async Task SetPlaytimeForRole(NetUserId player, string role, TimeSpan time)
        {
            await _db.SetPlaytimeForRole(player, role, time);
        }

        public async Task<TimeSpan?> GetPlaytimeForRole(NetUserId player, string role)
        {
            return await _db.GetPlaytimeForRole(player, role);
        }

        public async Task<Dictionary<string, TimeSpan>?> GetPlaytimeAllRoles(NetUserId player)
        {
            return await _db.GetPlaytimeAllRoles(player);
        }
    }
}
