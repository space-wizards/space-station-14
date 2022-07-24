using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.Players.PlayTimeTracking;

public sealed class PlayTimeTrackingManager
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ITaskManager _task = default!;

    private ISawmill _sawmill = default!;

    private TimeSpan _saveInterval;
    private TimeSpan _lastSave;

    private readonly List<Task> _pendingSaveTasks = new();

    /*
     * All of the reads below just read directly from PlayTimeInfo without touching the DB.
     * The writes update the timer and mark the tracker as dirty so it gets saved to the DB later.
     */

    private readonly Dictionary<NetUserId, PlayTimeInfo> _cachedPlayerData = new();

    public event Action? BeforeSave;

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
        if (_timing.RealTime < _lastSave + _saveInterval)
            return;

        Save();
    }

    public async void Save()
    {
        _lastSave = _timing.RealTime;

        BeforeSave?.Invoke();

        WaitPending(DoSaveAsync());
    }

    public async void SaveSession(IPlayerSession session)
    {
        // Yes this causes all sessions to be flushed by the tracking system: Ah well.
        BeforeSave?.Invoke();

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
            foreach (var tracker in data.TrackersDirty)
            {
                log.Add(new PlayTimeUpdate(player, tracker, data.Trackers[tracker]));
            }

            data.TrackersDirty.Clear();
        }

        await _db.UpdatePlayTimes(log);
    }

    private async Task DoSaveSessionAsync(IPlayerSession session)
    {
        var log = new List<PlayTimeUpdate>();

        foreach (var (player, data) in _cachedPlayerData)
        {
            foreach (var tracker in data.TrackersDirty)
            {
                log.Add(new PlayTimeUpdate(player, tracker, data.Trackers[tracker]));
            }

            data.TrackersDirty.Clear();
        }

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

        ref var timer = ref CollectionsMarshal.GetValueRefOrAddDefault(data.Trackers, tracker, out _);
        timer += time;

        data.TrackersDirty.Add(tracker);
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

    /// <summary>
    /// Play time info for a particular player.
    /// </summary>
    private sealed class PlayTimeInfo
    {
        /// <summary>
        /// Have we finished retrieving our data from the DB?
        /// </summary>
        public bool Initialized;

        public readonly Dictionary<string, TimeSpan> Trackers = new();

        public readonly HashSet<string> TrackersDirty = new();
    }
}
