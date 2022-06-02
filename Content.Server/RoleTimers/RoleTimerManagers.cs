using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;

namespace Content.Server.RoleTimers
{
    public sealed class RoleTimerManager
    {
        [Dependency] private readonly IServerDbManager _db = default!;

        private async Task<RoleTimer> GetRoleTimer(NetUserId userId, string role)
        {
            return await _db.GetRoleTimer(userId.UserId, role);
        }

        private async Task<List<RoleTimer>> GetRoleTimers(NetUserId userId)
        {
            return await _db.GetRoleTimers(userId.UserId);
        }

        private async Task EditRoleTimer(int id, TimeSpan time)
        {
            await _db.EditRoleTimer(id, time);
        }
    }
}
