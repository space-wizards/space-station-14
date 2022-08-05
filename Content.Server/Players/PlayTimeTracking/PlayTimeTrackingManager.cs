using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Exceptions;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Players.PlayTimeTracking;

public delegate void CalcPlayTimeTrackersCallback(in NetUserId player, HashSet<string> trackers);

public sealed class PlayTimeTrackingManager
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ITaskManager _task = default!;
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

    private ISawmill _sawmill = default!;

    private ValueList<NetUserId> _playersDirty;


    // DB saving logic.
    private TimeSpan _saveInterval;
    private TimeSpan _lastSave;

    private readonly List<Task> _pendingSaveTasks = new();

    /*
     * All of the reads below just read directly from PlayTimeInfo without touching the DB.
     * The writes update the timer and mark the tracker as dirty so it gets saved to the DB later.
     */

    private readonly Dictionary<NetUserId, PlayTimeInfo> _cachedPlayerData = new();

    public event CalcPlayTimeTrackersCallback? CalcTrackers;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("play_time");

        _net.RegisterNetMessage<MsgPlayTime>();

        _cfg.OnValueChanged(CCVars.GameRoleTimersSaveInterval, f => _saveInterval = TimeSpan.FromSeconds(f), true);
    }

    public void Shutdown()
    {
        Save();
        _task.BlockWaitOnTask(Task.WhenAll(_pendingSaveTasks));
    }

    public void Update()
    {
        RefreshDirtyTrackers();

        if (_timing.RealTime < _lastSave + _saveInterval)
            return;

        Save();
    }

    private void RefreshDirtyTrackers()
    {
        if (_playersDirty.Count == 0)
            return;

        var time = _timing.RealTime;

        foreach (var dirty in _playersDirty)
        {
            if (!_cachedPlayerData.TryGetValue(dirty, out var data))
                continue;

            DebugTools.Assert(data.TrackersDirty);

            RefreshSingleTracker(dirty, data, time);
        }

        _playersDirty.Clear();
    }

    private void RefreshSingleTracker(in NetUserId dirty, PlayTimeInfo data, TimeSpan time)
    {
        DebugTools.Assert(data.Initialized);

        FlushSingleTracker(data, time);

        data.TrackersDirty = false;

        data.ActiveTrackers.Clear();

        // Fetch new trackers.
        // Inside try catch to avoid state corruption from bad callback code.
        try
        {
            CalcTrackers?.Invoke(dirty, data.ActiveTrackers);
        }
        catch (Exception e)
        {
            _runtimeLog.LogException(e, "PlayTime CalcTrackers");
            data.ActiveTrackers.Clear();
        }
    }

    public void FlushAllTrackers()
    {
        var time = _timing.RealTime;

        foreach (var (userId, data) in _cachedPlayerData)
        {
            FlushSingleTracker(data, time);
        }
    }

    public void FlushTracker(IPlayerSession player)
    {
        var time = _timing.RealTime;
        var userId = player.UserId;
        var data = _cachedPlayerData[userId];

        FlushSingleTracker(data, time);
    }

    private static void FlushSingleTracker(PlayTimeInfo data, TimeSpan time)
    {
        var delta = time - data.LastUpdate;
        data.LastUpdate = time;

        // Flush active trackers into semi-permanent storage.
        foreach (var active in data.ActiveTrackers)
        {
            AddTimeToTracker(data, active, delta);
        }
    }

    public async void Save()
    {
        FlushAllTrackers();

        _lastSave = _timing.RealTime;

        WaitPending(DoSaveAsync());
    }

    public async void SaveSession(IPlayerSession session)
    {
        // This causes all trackers to refresh, ah well.
        FlushAllTrackers();

        WaitPending(DoSaveSessionAsync(session));
    }

    private async void WaitPending(Task task)
    {
        _pendingSaveTasks.Add(task);

        try
        {
            await task;
        }
        finally
        {
            _pendingSaveTasks.Remove(task);
        }
    }

    private async Task DoSaveAsync()
    {
        var log = new List<PlayTimeUpdate>();

        foreach (var (player, data) in _cachedPlayerData)
        {
            foreach (var tracker in data.DbTrackersDirty)
            {
                log.Add(new PlayTimeUpdate(player, tracker, data.Trackers[tracker]));
            }

            data.DbTrackersDirty.Clear();
        }

        await _db.UpdatePlayTimes(log);
    }

    private async Task DoSaveSessionAsync(IPlayerSession session)
    {
        var log = new List<PlayTimeUpdate>();

        var userId = session.UserId;
        var data = _cachedPlayerData[userId];

        foreach (var tracker in data.DbTrackersDirty)
        {
            log.Add(new PlayTimeUpdate(userId, tracker, data.Trackers[tracker]));
        }

        data.DbTrackersDirty.Clear();

        await _db.UpdatePlayTimes(log);
    }

    public async Task LoadData(IPlayerSession session, CancellationToken cancel)
    {
        var userId = session.UserId;

        var data = new PlayTimeInfo();
        _cachedPlayerData.Add(userId, data);

        var roleTimers = await _db.GetPlayTimes(userId);
        cancel.ThrowIfCancellationRequested();

        foreach (var timer in roleTimers)
        {
            data.Trackers.Add(timer.Tracker, timer.TimeSpent);
        }

        data.Initialized = true;

        QueueRefreshTrackers(session);
    }

    public void ClientDisconnected(IPlayerSession session)
    {
        SaveSession(session);

        _cachedPlayerData.Remove(session.UserId);
    }

    public void SendRoleTimers(IPlayerSession pSession)
    {
        var roles = GetTrackerTimes(pSession.UserId);

        var msg = new MsgPlayTime
        {
            Trackers = roles
        };

        _net.ServerSendMessage(msg, pSession.ConnectedClient);
    }

    public void AddTimeToTracker(NetUserId id, string tracker, TimeSpan time)
    {
        if (!_cachedPlayerData.TryGetValue(id, out var data) || !data.Initialized)
            throw new InvalidOperationException("Play time info is not yet loaded for this player!");

        AddTimeToTracker(data, tracker, time);
    }

    private static void AddTimeToTracker(PlayTimeInfo data, string tracker, TimeSpan time)
    {
        ref var timer = ref CollectionsMarshal.GetValueRefOrAddDefault(data.Trackers, tracker, out _);
        timer += time;

        data.DbTrackersDirty.Add(tracker);
    }

    public void AddTimeToOverallPlaytime(NetUserId id, TimeSpan time)
    {
        AddTimeToTracker(id, PlayTimeTrackingShared.TrackerOverall, time);
    }

    public TimeSpan GetOverallPlaytime(NetUserId id)
    {
        return GetPlayTimeForTracker(id, PlayTimeTrackingShared.TrackerOverall);
    }

    public Dictionary<string, TimeSpan> GetTrackerTimes(NetUserId id)
    {
        if (!_cachedPlayerData.TryGetValue(id, out var data) || !data.Initialized)
            throw new InvalidOperationException("Play time info is not yet loaded for this player!");

        return data.Trackers;
    }

    public TimeSpan GetPlayTimeForTracker(NetUserId id, string tracker)
    {
        if (!_cachedPlayerData.TryGetValue(id, out var data) || !data.Initialized)
            throw new InvalidOperationException("Play time info is not yet loaded for this player!");

        return data.Trackers.GetValueOrDefault(tracker);
    }

    public void QueueRefreshTrackers(IPlayerSession player)
    {
        var userId = player.UserId;
        if (!_cachedPlayerData.TryGetValue(userId, out var data))
            return;

        if (data.TrackersDirty || !data.Initialized)
            return;

        data.TrackersDirty = true;
        _playersDirty.Add(userId);
    }

    /// <summary>
    /// Play time info for a particular player.
    /// </summary>
    private sealed class PlayTimeInfo
    {
        // Active tracking info

        public bool TrackersDirty;
        public readonly HashSet<string> ActiveTrackers = new();
        public TimeSpan LastUpdate;

        // Stored tracked time info.

        /// <summary>
        /// Have we finished retrieving our data from the DB?
        /// </summary>
        public bool Initialized;

        public readonly Dictionary<string, TimeSpan> Trackers = new();

        /// <summary>
        /// Set of trackers which are different from their DB values and need to be saved to DB.
        /// </summary>
        public readonly HashSet<string> DbTrackersDirty = new();
    }
}
