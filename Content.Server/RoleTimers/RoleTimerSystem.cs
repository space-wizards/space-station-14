using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.RoleTimers
{
    public sealed class RoleTimerSystem : EntitySystem
    {
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly PrototypeManager _prototypeManager = default!;

        private const int StateCheckTime = 90;
        private Dictionary<IPlayerSession, CachedPlayerRoleTimers> _cachedPlayerData = new();

        public override void Initialize()
        {
            base.Initialize();
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => SaveAndClear());
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            switch(args.NewStatus)
            {
                case SessionStatus.Connected:
                {
                    CachePlayerRoles(args.Session);
                    break;
                }
                case SessionStatus.Disconnected:
                {
#pragma warning disable CS4014
                    SaveRoleTime(args.Session, DateTime.Now, GetTimeForCurrentRole(args.Session),
                    _cachedPlayerData[args.Session].CurrentRole);
#pragma warning restore CS4014
                    _cachedPlayerData.Remove(args.Session);
                    break;
                }
            }
            // Make sure we save and remove any disconnected players from the cache
            if(args.NewStatus == SessionStatus.Disconnected)
            {
#pragma warning disable CS4014
                SaveRoleTime(args.Session, DateTime.Now, GetTimeForCurrentRole(args.Session),
                    _cachedPlayerData[args.Session].CurrentRole);
#pragma warning restore CS4014
                _cachedPlayerData.Remove(args.Session);
            }
        }

        /// <summary>
        /// Saves and clears everything from cached player data
        /// </summary>
        private void SaveAndClear()
        {
            foreach (var (player, data) in _cachedPlayerData)
            {
#pragma warning disable CS4014
                SaveRoleTime(player, DateTime.Now, GetTimeForCurrentRole(player), data.CurrentRole);
#pragma warning restore CS4014
            }
            _cachedPlayerData.Clear();
        }

        public async Task<bool> CanPlayRole(IPlayerSession player, string role)
        {
            return await CanPlayRole(player.UserId, role);
        }

        public async Task<bool> CanPlayRole(NetUserId player, string role)
        {
            var roleProto = _prototypeManager.Index<JobPrototype>(role);
            if (roleProto.Requirements == null) return true;
            foreach (var requirement in roleProto.Requirements)
            {
                var job = requirement.Job;
                // TODO: There's probably a better way to do this...
                if(job == null) continue;
                var time = await _db.AddOrGetRoleTimer(player, job);
                if (time.TimeSpent <= requirement.Time)
                {
                    return false;
                }
            }
            return true;
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
                var cachedPlayerRoleTimers = _cachedPlayerData[player];
                cachedPlayerRoleTimers.CurrentRole = timer.Role;
                cachedPlayerRoleTimers.RoleTimers[timer.Role] = now;
            }
            // Save role time
            if (GetTimeForCurrentRole(player) != null)
            {
                await SaveRoleTime(player, now, GetTimeForCurrentRole(player)!.Value);
            }
            // new role
            //var rtimer = await _db.AddOrGetRoleTimer(player.UserId, role);
            //_cachedPlayerData[player] = new Tuple<string, DateTime>(role, now);
        }

        private async Task SaveRoleTime(IPlayerSession player, DateTime now, DateTime then)
        {
            var rtimer = await _db.AddOrGetRoleTimer(player.UserId, role);
            var between = now.Subtract(then);
            var newtime = rtimer.TimeSpent.Add(between);
            await _db.EditRoleTimer(rtimer.Id, newtime);
        }

        /// <summary>
        /// Gets the time played for the currently selected role.
        /// </summary>
        /// <param name="player">The player to target.</param>
        /// <returns>The time or null if no role is selected at the moment.</returns>
        private DateTime? GetTimeForCurrentRole(IPlayerSession player)
        {
            var currentRole = _cachedPlayerData[player].CurrentRole;
            if (currentRole != null)
                return _cachedPlayerData[player].RoleTimers[currentRole];
            return null;
        }

        private async Task CachePlayerRoles(IPlayerSession player)
        {
            var cacheObject = new CachedPlayerRoleTimers();
            var query = await _db.GetRoleTimers(player.UserId);
            foreach (var timer in query)
            {
                if (cacheObject.RoleTimers != null) cacheObject.RoleTimers[timer.Role] = DateTime.Now;
            }
        }
    }

    public struct CachedPlayerRoleTimers
    {
        public string? CurrentRole;
        // The reasoning for having a DateTime here is that we don't need to update it, and
        // can instead just figure out how much time has passed since they first joined and now,
        // and use that to get the TimeSpan to add onto the saved playtime
        public Dictionary<string, DateTime> RoleTimers;
    }
}
