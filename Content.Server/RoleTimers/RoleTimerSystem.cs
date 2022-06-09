using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Server.RoleTimers
{
    public sealed class RoleTimerSystem : EntitySystem
    {
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private const int StateCheckTime = 90;
        private Dictionary<IPlayerSession, CachedPlayerRoleTimers> _cachedPlayerData = new();
        private Dictionary<string, HashSet<JobRequirement>> _cachedJobRequirements = new();

        public override void Initialize()
        {
            base.Initialize();
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => SaveAndClear());

            foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if(job.Requirements == null) continue;
                _cachedJobRequirements[job.Name] = job.Requirements;
            }
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            switch(args.NewStatus)
            {
                case SessionStatus.Connected:
                {
#pragma warning disable CS4014
                    CachePlayerRoles(args.Session);
#pragma warning restore CS4014
                    break;
                }
                case SessionStatus.Disconnected:
                {
                    if (!IsPlayerTimeCachedYet(args.Session))
                    {
                        return;
                    }
                    var time = GetTimeForCurrentRole(args.Session);
                    if (time == null) return;
#pragma warning disable CS4014
                    SaveRoleTime(args.Session, DateTime.Now, time.Value);
#pragma warning restore CS4014
                    _cachedPlayerData.Remove(args.Session);
                    break;
                }
            }
        }

        /// <summary>
        /// Saves and clears everything from cached player data
        /// </summary>
        private void SaveAndClear()
        {
            foreach (var (player, _) in _cachedPlayerData)
            {
                var time = GetTimeForCurrentRole(player);
                if (time == null) continue;
#pragma warning disable CS4014
                SaveRoleTime(player, DateTime.Now, time.Value);
#pragma warning restore CS4014
            }
            _cachedPlayerData.Clear();
        }

        public bool CanPlayRole(IPlayerSession player, string role)
        {
            var requirements = _cachedJobRequirements[role];
            foreach (var requirement in requirements)
            {
                var job = requirement.Job;
                if (!IsPlayerTimeCachedYet(player))
                {
                    Logger.ErrorS("RoleTimers", $"Tried to check if an uncached player ({player} {player.UserId} could play role {role}");
                    return false;
                }
                var time = _cachedPlayerData[player].RoleTimers[job].Item2;
                if (time <= requirement.Time)
                {
                    return false;
                }
            }
            return true;
        }

        public HashSet<string> GetRestrictedRoles(IPlayerSession player)
        {
            var restricted = new HashSet<string>();
            var roles = _cachedJobRequirements.Keys;
            foreach (var role in roles)
            {
                if (CanPlayRole(player, role)) continue;
                restricted.Add(role);
            }

            return restricted;
        }

        public bool IsPlayerTimeCachedYet(IPlayerSession player)
        {
            return _cachedPlayerData.ContainsKey(player);
        }

        public Dictionary<string, Tuple<DateTime, TimeSpan>>? GetCachedRoleTimers(IPlayerSession player)
        {
            if (!IsPlayerTimeCachedYet(player))
            {
                return null;
            }

            return _cachedPlayerData[player].RoleTimers;
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
            if (!IsPlayerTimeCachedYet(player) && role != null)
            {
                var timer = await _db.AddOrGetRoleTimer(player.UserId, role);
                var cachedPlayerRoleTimers = _cachedPlayerData[player];
                cachedPlayerRoleTimers.CurrentRole = timer.Role;
                cachedPlayerRoleTimers.RoleTimers[timer.Role] = new Tuple<DateTime, TimeSpan>(now, timer.TimeSpent);
            }
            // Save role time
            var time = GetTimeForCurrentRole(player);
            if (time != null)
            {
                await SaveRoleTime(player, now, time.Value);
            }
            // New role
            if (role == null)
            {
                _cachedPlayerData[player].SetCurrentRole(null);
            }
            var rtimer = await _db.AddOrGetRoleTimer(player.UserId, role!);
            _cachedPlayerData[player].SetCurrentRole(role);
        }

        private async Task SaveRoleTime(IPlayerSession player, DateTime now, DateTime then)
        {
            var currentRole = _cachedPlayerData[player].CurrentRole;
            if (currentRole == null) return;
            var rtimer = await _db.AddOrGetRoleTimer(player.UserId, currentRole);
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
            if (IsPlayerTimeCachedYet(player))
            {
                Logger.ErrorS("RoleTimers", $"Tried to get the time for a role from an uncached player ({player} {player.UserId})");
                return null;
            }
            var currentRole = _cachedPlayerData[player].CurrentRole;
            if (currentRole != null)
                return _cachedPlayerData[player].RoleTimers[currentRole].Item1;
            return null;
        }

        private async Task CachePlayerRoles(IPlayerSession player)
        {
            var cacheObject = new CachedPlayerRoleTimers();
            var query = await _db.GetRoleTimers(player.UserId);
            cacheObject.RoleTimers ??= new Dictionary<string, Tuple<DateTime, TimeSpan>>();
            foreach (var timer in query)
            {
                cacheObject.RoleTimers[timer.Role] = new Tuple<DateTime, TimeSpan>(DateTime.Now, timer.TimeSpent);
            }

            _cachedPlayerData[player] = cacheObject;
        }
    }

    public struct CachedPlayerRoleTimers
    {
        public string? CurrentRole;
        // The reasoning for having a DateTime here is that we don't need to update it, and
        // can instead just figure out how much time has passed since they first joined and now,
        // and use that to get the TimeSpan to add onto the saved playtime
        public Dictionary<string, Tuple<DateTime, TimeSpan>> RoleTimers;

        public void SetCurrentRole(string? role)
        {
            CurrentRole = role;
        }
    }
}
