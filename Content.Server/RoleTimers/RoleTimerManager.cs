using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.RoleTimers
{
    public sealed class RoleTimerManager : IEntityEventSubscriber
    {
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        // Shouldn't need concurrentdictionary as we're only modifying the value internally.
        private readonly Dictionary<NetUserId, PlayerRoleTimers> _cachedPlayerData = new();

        public void Initialize()
        {
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            var id = args.Session.UserId;
            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                    var data = _cachedPlayerData.GetOrNew(args.Session.UserId);
                    await CachePlayerRoles(id, data);
                    break;
                case SessionStatus.Disconnected:
                {
                    _cachedPlayerData.Remove(id, out _);
                    break;
                }
            }
        }

        /// <summary>
        /// Puts all relevant database information for a player into the cache.
        /// </summary>
        private async Task CachePlayerRoles(NetUserId player, PlayerRoleTimers timers)
        {
            if (timers.Initialized) return;
            var roleTimers = await _db.GetRoleTimers(player);
            var playtime = await _db.GetOverallPlayTime(player);

            foreach (var timer in roleTimers)
            {
                await timers.AddPlaytimeForRole(timer.Role, timer.TimeSpent);
            }

            await timers.AddOverallPlaytime(playtime);
            timers.Initialized = true;
        }

        public async void AddTimeToRole(NetUserId id, string role, TimeSpan time)
        {
            if (!_cachedPlayerData.TryGetValue(id, out var timers))
                return;

            var totalTime = await timers.AddPlaytimeForRole(role, time);

            var query = await _db.CreateOrGetRoleTimer(id, role);
            await _db.SetRoleTime(query.Id, totalTime);
        }

        public async void AddTimeToOverallPlaytime(NetUserId id, TimeSpan time)
        {
            if (!_cachedPlayerData.TryGetValue(id, out var timers))
                return;

            var playtime = await timers.AddOverallPlaytime(time);
            await _db.SetOverallPlayTime(id, playtime);
        }

        public async Task<TimeSpan> GetOverallPlaytime(IPlayerSession pSession)
        {
            if (!_cachedPlayerData.TryGetValue(pSession.UserId, out var timer)) return TimeSpan.Zero;
            return await timer.GetOverallTime();
        }

        public async Task<Dictionary<string, TimeSpan>> GetRolePlaytimes(IPlayerSession pSession)
        {
            if (!_cachedPlayerData.TryGetValue(pSession.UserId, out var timer)) return new Dictionary<string, TimeSpan>();
            return await timer.GetRoleTimers();
        }

        public async Task<TimeSpan> GetPlayTimeForRole(IPlayerSession pSession, string role)
        {
            if (!_cachedPlayerData.TryGetValue(pSession.UserId, out var timers))
            {
                return TimeSpan.Zero;
            }

            return await timers.GetPlaytimeForRole(role);
        }

        /// <summary>
        /// Role timers for a particular player.
        /// </summary>
        private sealed class PlayerRoleTimers
        {
            /// <summary>
            /// Have we already retrieved our data from the DB?
            /// </summary>
            public bool Initialized = false;

            private readonly Dictionary<string, TimeSpan> _roleTimers = new();
            private TimeSpan _overallPlaytime = TimeSpan.Zero;

            private readonly SemaphoreSlim _semaphore = new(1);

            // Semaphore everywhere coz DB writes might still be happening and adjusting.

            public async Task<TimeSpan> GetOverallTime()
            {
                await _semaphore.WaitAsync();
                var overall = _overallPlaytime;
                _semaphore.Release();
                return overall;
            }

            public async Task<Dictionary<string, TimeSpan>> GetRoleTimers()
            {
                await _semaphore.WaitAsync();
                var copied = new Dictionary<string, TimeSpan>(_roleTimers.Count);

                foreach (var (role, time) in _roleTimers)
                {
                    copied[role] = time;
                }

                _semaphore.Release();
                return copied;
            }

            public async Task<TimeSpan> AddOverallPlaytime(TimeSpan time)
            {
                await _semaphore.WaitAsync();
                var overallTime = _overallPlaytime + time;
                _overallPlaytime = overallTime;
                _semaphore.Release();
                return overallTime;
            }

            public async Task <TimeSpan> AddPlaytimeForRole(string role, TimeSpan time)
            {
                await _semaphore.WaitAsync();
                var existing = _roleTimers.GetOrNew(role);
                var newTime = existing + time;
                _roleTimers[role] = newTime;
                _semaphore.Release();
                return newTime;
            }

            public async Task<TimeSpan> GetPlaytimeForRole(string role)
            {
                await _semaphore.WaitAsync();
                _roleTimers.TryGetValue(role, out var time);
                _semaphore.Release();
                return time;
            }
        }
    }
}
