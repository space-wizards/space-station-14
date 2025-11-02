using Content.Server.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Antag;

/// <summary>
/// Manages saving and retrieving the last time that a player rolled any antag.
/// </summary>
// ported from https://github.com/Goob-Station/Goob-Station/blob/f6453f5ce37af138d28b8077ef33b0282d2bc52d/Content.Server/_Goobstation/Antag/LastRolledAntagManager.cs
// with permission from the singular codeowner at the point of that commit
public sealed class LastRolledAntagManager : IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly UserDbDataManager _userDbDataManager = default!;

    private readonly List<Task> _pendingSaveTasks = new();

    /// <summary>Cache of players and the last time they rolled antag.</summary>
    private readonly Dictionary<NetUserId, TimeSpan> _lastRolledData = new();

    private ISawmill _sawmill = default!;

    /// <summary>
    /// Saves last rolled values to the database before allowing the server to shutdown.
    /// </summary>
    public void Shutdown()
    {
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

        var lastRolledTimespan = await GetLastRolledAsync(userId);
        _lastRolledData.Add(userId, lastRolledTimespan);
    }

    public void ClientDisconnected(ICommonSession session)
    {
        _lastRolledData.Remove(session.UserId);
    }

    /// <inheritdoc cref="SetTimeAsyncInternal(NetUserId, TimeSpan, TimeSpan?)"/>
    public void SetLastRolled(NetUserId userId, TimeSpan to)
    {
        Task.Run(() => SetTimeAsync(userId, to));
    }

    /// <inheritdoc cref="GetLastRolledAsync(NetUserId)"/>
    public TimeSpan GetLastRolled(NetUserId userId)
    {
        return Task.Run(() => GetLastRolledAsync(userId)).GetAwaiter().GetResult();
    }

    #region Internal/Async tasks

    /// <summary>
    /// Sets a player's last rolled antag time.
    /// </summary>
    /// <param name="oldTime">Optional parameter used to provide more data for logging.</param>
    private async Task SetTimeAsyncInternal(NetUserId userId, TimeSpan time, TimeSpan? oldTime = null)
    {
        var setTimeTask = _dbManager.SetLastRolledAntag(userId, time);
        TrackPending(setTimeTask); // Track the Task<bool>
        var success = await setTimeTask;

        if (success)
        {
            // only set on db-operation success, since ideally we would want to keep db state and _lastRolledData state synchronised
            _lastRolledData[userId] = time;
            _sawmill.Debug($"Successfully set LastRolledAntag for {userId} from {oldTime?.ToString() ?? "N/A"} to {time}");
        }
        else
            _sawmill.Error($"Failed to set LastRolledAntag for {userId}. Player not found or other issue.");
    }

    /// <inheritdoc cref="SetTimeAsyncInternal(NetUserId, TimeSpan, TimeSpan?)"/>
    private async Task SetTimeAsync(NetUserId userId, TimeSpan to)
    {
        await SetTimeAsyncInternal(userId, to, await GetLastRolledAsync(userId));
    }

    /// <summary>
    /// Gets a player's last rolled antag time.
    /// </summary>
    private async Task<TimeSpan> GetLastRolledAsync(NetUserId userId)
    {
        return await _dbManager.GetLastRolledAntag(userId);
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
