using Content.Server.Database;
using Robust.Shared.Network;

namespace Content.Server.RoleTimers
{
    public sealed class RoleTimerManager
    {
        [Dependency] private readonly IServerDbManager _db = default!;

        private RoleTimer GetRoleTimer(NetUserId userId, string role)
        {
            return _db.GetRoleTimer(userId.UserId, role).Result;
        }

        private List<RoleTimer> GetRoleTimers(NetUserId userId)
        {
            return _db.GetRoleTimers(userId.UserId).Result;
        }

        private void EditRoleTimer(int id, TimeSpan time)
        {
            _db.EditRoleTimer(id, time);
        }
    }
}
