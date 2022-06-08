using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Server.Player;
using Robust.Shared.Enums;

namespace Content.Server.RoleTimers
{
    public sealed class RoleTimerManager : IEntityEventSubscriber
    {
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private const int StateCheckTime = 90;
        // The reasoning for having a DateTime here is that we don't need to update it, and
        // can instead just figure out how much time has passed since they first joined and now,
        // and use that to get the TimeSpan to add onto the saved playtime
        private Dictionary<IPlayerSession, Tuple<string, DateTime>> _cachedPlayerData = new();

        public void Initialize()
        {
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            // Make sure we save and remove any disconnected players from the cache
            if(args.NewStatus == SessionStatus.Disconnected)
            {
#pragma warning disable CS4014
                SaveRoleTime(args.Session, DateTime.Now, _cachedPlayerData[args.Session].Item2,
                    _cachedPlayerData[args.Session].Item1);
#pragma warning restore CS4014
                _cachedPlayerData.Remove(args.Session);
            }
        }

        public async Task RoleChange(IPlayerSession player, string role, DateTime now)
        {
            // If the role doesn't exist in the cache, load it
            if (!_cachedPlayerData.ContainsKey(player))
            {
                var timer = await _db.AddOrGetRoleTimer(player.UserId, role);
                _cachedPlayerData[player] = new Tuple<string, DateTime>(timer.Role, now);
            }
            // Save role time
            await SaveRoleTime(player, now, _cachedPlayerData[player].Item2);
            // New role
            var rtimer = await _db.AddOrGetRoleTimer(player.UserId, role);
            _cachedPlayerData[player] = new Tuple<string, DateTime>(role, now);
        }

        private async Task SaveRoleTime(IPlayerSession player, DateTime now, DateTime then, string? role = null)
        {
            role ??= _cachedPlayerData[player].Item1;
            var rtimer = await _db.AddOrGetRoleTimer(player.UserId, role);
            var between = now.Subtract(then);
            var newtime = rtimer.TimeSpent.Add(between);
            await _db.EditRoleTimer(rtimer.Id, newtime);
        }
    }
}
