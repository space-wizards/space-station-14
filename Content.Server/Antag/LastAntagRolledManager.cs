using Content.Server.Database;
using Robust.Shared.Asynchronous;
using Robust.Shared.Network;
using System.Threading.Tasks;

namespace Content.Server.Antag;

/// <summary>
/// Manages saving and retrieving the last time that a player rolled any antag.
/// </summary>
// ported from https://github.com/Goob-Station/Goob-Station/blob/5e59f3a65991e805ee94c857a373857c9282b4d5/Content.Server/_Goobstation/Antag/LastRolledAntagManager.cs
// with permission from codeowner
public sealed class LastRolledAntagManager
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    private readonly List<Task> _pendingSaveTasks = new();
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("last_antag");
    }

    /// <summary>
    /// Saves last rolled values to the database before allowing the server to shutdown.
    /// </summary>
    public void Shutdown()
    {
        _taskManager.BlockWaitOnTask(Task.WhenAll(_pendingSaveTasks));
    }

    /// <summary>
    /// Sets a player's last rolled antag time.
    /// </summary>
    public TimeSpan SetLastRolled(NetUserId userId, TimeSpan to)
    {
        var oldTime = Task.Run(() => SetTimeAsync(userId, to)).GetAwaiter().GetResult();
        _sawmill.Info($"Setting {userId} last rolled antag to {to} from {oldTime}");
        return oldTime;
    }

    /// <summary>
    /// Gets a player's last rolled antag time.
    /// </summary>
    public TimeSpan GetLastRolled(NetUserId userId)
    {
        return Task.Run(() => GetTimeAsync(userId)).GetAwaiter().GetResult();
    }

    #region Internal/Async tasks

    /// <summary>
    /// Sets a player's last rolled antag time.
    /// </summary>
    private async Task SetTimeAsyncInternal(NetUserId userId, TimeSpan time, TimeSpan oldTime)
    {
        var setTimeTask = _dbManager.SetLastRolledAntag(userId, time);
        TrackPending(setTimeTask); // Track the Task<bool>
        var success = await setTimeTask;

        if (success)
            _sawmill.Debug($"Successfully set LastRolledAntag for {userId} from {oldTime} to {time}");
        else
            _sawmill.Error($"Failed to set LastRolledAntag for {userId}. Player not found or other issue.");
    }

    /// <summary>
    /// Sets a player's last rolled antag time.
    /// </summary>
    private async Task<TimeSpan> SetTimeAsync(NetUserId userId, TimeSpan to)
    {
        var oldTime = GetLastRolled(userId);
        await SetTimeAsyncInternal(userId, to, oldTime);
        return oldTime;
    }

    /// <summary>
    /// Gets a player's last rolled antag time.
    /// </summary>
    private async Task<TimeSpan> GetTimeAsync(NetUserId userId)
    {
        return await _dbManager.GetLastRolledAntag(userId);
    }

    /// <summary>
    /// Track a database save task to make sure we block server shutdown on it.
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
