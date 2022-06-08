using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Enums;

namespace Content.Server.RoleTimers
{
    public sealed class RoleTimerSystem : EntitySystem
    {
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private const int StateCheckTime = 90;
        // The reasoning for having a DateTime here is that we don't need to update it, and
        // can instead just figure out how much time has passed since they first joined and now,
        // and use that to get the TimeSpan to add onto the saved playtime
        private Dictionary<IPlayerSession, Tuple<string, DateTime>> _cachedPlayerData = new();

        public override void Initialize()
        {
            base.Initialize();
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => SaveAndClear());
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

        /// <summary>
        /// Saves and clears everything from cached player data
        /// </summary>
        public void SaveAndClear()
        {
            foreach (var (player, data) in _cachedPlayerData)
            {
                SaveRoleTime(player, DateTime.Now, data.Item2, data.Item1);
            }
            _cachedPlayerData.Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="player"></param>
        /// <param name="role">The role to change to. Set to null to not count anything.</param>
        /// <param name="now"></param>
        public async Task RoleChange(IPlayerSession player, string? role, DateTime now)
        {
            // If the role doesn't exist in the cache, load it
            if (!_cachedPlayerData.ContainsKey(player) && role != null)
            {
                var timer = await _db.AddOrGetRoleTimer(player.UserId, role);
                _cachedPlayerData[player] = new Tuple<string, DateTime>(timer.Role, now);
            }
            // Save role time
            await SaveRoleTime(player, now, _cachedPlayerData[player].Item2);
            if (role == null)
            {
                _cachedPlayerData.Remove(player);
                return;
            }
            // new role
            //var rtimer = await _db.AddOrGetRoleTimer(player.UserId, role);
            //_cachedPlayerData[player] = new Tuple<string, DateTime>(role, now);
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
