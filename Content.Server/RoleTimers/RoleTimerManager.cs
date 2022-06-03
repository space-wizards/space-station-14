using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.RoleTimers
{
    public sealed class RoleTimerManager : IEntityEventSubscriber
    {
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private const int StateCheckTime = 90;
        private Dictionary<IPlayerSession, Tuple<string, DateTime>> _playerData = new();

        public void Initialize()
        {
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {

        }

        public async Task<RoleTimer> GetRoleTimer(NetUserId userId, string role)
        {
            return await _db.GetRoleTimer(userId.UserId, role);
        }

        public async Task<List<RoleTimer>> GetRoleTimers(NetUserId userId)
        {
            return await _db.GetRoleTimers(userId.UserId);
        }

        public async Task EditRoleTimer(int id, TimeSpan time)
        {
            await _db.EditRoleTimer(id, time);
        }
    }
}
