using System.Threading;
using System.Threading.Tasks;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Database;

/// <summary>
/// Manages per-user data that comes from the database. Ensures it is loaded efficiently on client connect,
/// and ensures data is loaded before allowing players to spawn or such.
/// </summary>
/// <remarks>
/// Actual loading code is handled by separate managers such as <see cref="IServerPreferencesManager"/>.
/// This manager is simply a centralized "is loading done" controller for other code to rely on.
/// </remarks>
public sealed class UserDbDataManager : IPostInjectInit
{
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    private readonly Dictionary<NetUserId, UserData> _users = new();

    private ISawmill _sawmill = default!;

    // TODO: Ideally connected/disconnected would be subscribed to IPlayerManager directly,
    // but this runs into ordering issues with game ticker.
    public void ClientConnected(ICommonSession session)
    {
        _sawmill.Verbose($"Initiating load for user {session}");

        DebugTools.Assert(!_users.ContainsKey(session.UserId), "We should not have any cached data on client connect.");

        var cts = new CancellationTokenSource();
        var task = Load(session, cts.Token);
        var data = new UserData(cts, task);

        _users.Add(session.UserId, data);
    }

    public void ClientDisconnected(ICommonSession session)
    {
        _users.Remove(session.UserId, out var data);
        if (data == null)
            throw new InvalidOperationException("Did not have cached data in ClientDisconnect!");

        data.Cancel.Cancel();
        data.Cancel.Dispose();

        _prefs.OnClientDisconnected(session);
        _playTimeTracking.ClientDisconnected(session);
    }

    private async Task Load(ICommonSession session, CancellationToken cancel)
    {
        // The task returned by this function is only ever observed by callers of WaitLoadComplete,
        // which doesn't even happen currently if the lobby is enabled.
        // As such, this task must NOT throw a non-cancellation error!
        try
        {
            await Task.WhenAll(
                _prefs.LoadData(session, cancel),
                _playTimeTracking.LoadData(session, cancel));

            cancel.ThrowIfCancellationRequested();
            _prefs.FinishLoad(session);

            _sawmill.Verbose($"Load complete for user {session}");
        }
        catch (OperationCanceledException)
        {
            _sawmill.Debug($"Load cancelled for user {session}");

            // We can rethrow the cancellation.
            // This will make the task returned by WaitLoadComplete() also return a cancellation.
            throw;
        }
        catch (Exception e)
        {
            // Must catch all exceptions here, otherwise task may go unobserved.
            _sawmill.Error($"Load of user data failed: {e}");

            // Kick them from server, since something is hosed. Let them try again I guess.
            session.Channel.Disconnect("Loading of server user data failed, this is a bug.");

            // We throw a OperationCanceledException so users of WaitLoadComplete() always see cancellation here.
            throw new OperationCanceledException("Load of user data cancelled due to unknown error");
        }
    }

    /// <summary>
    /// Wait for all on-database data for a user to be loaded.
    /// </summary>
    /// <remarks>
    /// The task returned by this function may end up in a cancelled state
    /// (throwing <see cref="OperationCanceledException"/>) if the user disconnects while loading or an error occurs.
    /// </remarks>
    /// <param name="session"></param>
    /// <returns>
    /// A task that completes when all on-database data for a user has finished loading.
    /// </returns>
    public Task WaitLoadComplete(ICommonSession session)
    {
        return _users[session.UserId].Task;
    }

    public bool IsLoadComplete(ICommonSession session)
    {
        return GetLoadTask(session).IsCompletedSuccessfully;
    }

    public Task GetLoadTask(ICommonSession session)
    {
        return _users[session.UserId].Task;
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("userdb");
    }

    private sealed record UserData(CancellationTokenSource Cancel, Task Task);
}
