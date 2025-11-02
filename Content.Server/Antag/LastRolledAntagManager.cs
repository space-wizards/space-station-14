using Content.Server.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Antag;

/// <summary>
/// Manages saving and retrieving the last time that a player rolled any antag.
/// For every query, an internal cache is used rather than querying the DB.
/// </summary>
/*
    mostly ported from https://github.com/Goob-Station/Goob-Station/blob/f6453f5ce37af138d28b8077ef33b0282d2bc52d/Content.Server/_Goobstation/Antag/LastRolledAntagManager.cs
    with permission from the singular codeowner at the point of that commit

    this uses a model of operations stolen from PlaytimeTrackingManager:
        - for every player joined, their last-rolled-antag time is read from the db and stored in an internal cache

        - when something calls the public method to access their last-rolled time, the internal cache is referenced
        - when the player leaves:
        - - if their last-rolled time was modified since their join,
        - - - it is saved in the DB
        - - if it wasn't, then nothing happens
        - - the player's data is then removed from the internal cache
        - everything is stored to the DB when the manager shuts down
*/
public sealed class LastRolledAntagManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly UserDbDataManager _userDbDataManager = default!;

    private readonly List<Task> _pendingSaveTasks = new();

    /// <summary>Cache of players and the last time they rolled antag.</summary>
    private readonly Dictionary<NetUserId, TimeSpan> _lastRolledData = new();

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("last_rolled_antag");
    }

    /// <summary>
    /// Saves last rolled values to the database before allowing the server to shutdown.
    /// </summary>
    public void Shutdown()
    {
        Save();
        _taskManager.BlockWaitOnTask(Task.WhenAll(_pendingSaveTasks));
    }

    void IPostInjectInit.PostInject()
    {
        _userDbDataManager.AddOnLoadPlayer(LoadData);
        _userDbDataManager.AddOnPlayerDisconnect(ClientDisconnected);
    }

    public async Task LoadData(ICommonSession session, CancellationToken token)
    {
        var userId = session.UserId;
        var lastRolledTimespan = await _dbManager.GetLastTimeAntagRolled(userId);
        _sawmill.Debug($"Successfully retrieved LastRolledAntag for {userId}; value: {lastRolledTimespan}");

        _lastRolledData.Add(userId, lastRolledTimespan);
    }

    public void ClientDisconnected(ICommonSession session)
    {
        var userId = session.UserId;
        SaveSession(userId);

        _lastRolledData.Remove(userId);
    }

    /// <inheritdoc cref="SaveSessionAsync(NetUserId, TimeSpan)"/>
    /// <param name="savedLastRolledTime">If null, defaults to the last-rolled time in the internal cache.</param>
    public void SaveSession(NetUserId userId, TimeSpan? savedLastRolledTime = null)
    {
        savedLastRolledTime ??= _lastRolledData.GetValueOrDefault(userId);
        TrackPending(SaveSessionAsync(userId, savedLastRolledTime.Value));
    }


    /// <summary>
    /// Save all modified time trackers for all players to the database.
    /// Saves the last-rolled time of each player as the last-rolled time in the internal cache.
    /// </summary>
    public void Save()
    {
        TrackPending(SaveAllAsync());
    }

    /// <summary>
    /// Saves a player's last rolled antag time to the internal cache.
    /// </summary>
    public void SetLastRolled(NetUserId userId, TimeSpan value)
    {
        _lastRolledData[userId] = value;
    }

    /// <summary>
    /// Gets a player's last rolled antag time from the internal cache.
    /// </summary>
    public TimeSpan GetLastRolled(NetUserId userId)
    {
        return _lastRolledData.GetValueOrDefault(userId);
    }

    #region Internal/Async tasks

    /// <summary>
    /// Saves the last-rolled-antag time for the player, to the database.
    /// This does not update the internal cache (<see cref="_lastRolledData"/>),
    /// but may query it.
    /// </summary>
    private async Task SaveSessionAsync(NetUserId userId, TimeSpan savedLastRolledTime)
    {
        var setTimeTask = _dbManager.SetLastRolledAntag(userId, savedLastRolledTime);
        TrackPending(setTimeTask); // Track the Task<bool>
        var success = await setTimeTask;

        if (success)
            _sawmill.Debug($"Successfully saved LastRolledAntag for {userId} from {_lastRolledData.GetValueOrDefault(userId)} to {savedLastRolledTime}.");
        else
            _sawmill.Error($"Failed to save LastRolledAntag for {userId}. Player not found or other issue.");
    }

    /// <summary>
    /// Saves the last-rolled-antag time for every player, to the database.
    /// Uses the internally cached last-rolled time to save.
    /// </summary>
    private async Task SaveAllAsync()
    {
        // variable assign is to silence CS4014: "Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the await operator to the result of the call."
        Task saveTask;
        foreach (var (userId, lastRolledTime) in _lastRolledData)
            saveTask = SaveSessionAsync(userId, lastRolledTime);
    }

    /// <summary>
    /// Track a database save task to make sure we block server shutdown incase both are happening simultaneously.
    /// </summary>
    private async void TrackPending(Task task)
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

    #endregion
}
