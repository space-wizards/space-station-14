using System.Threading;
using System.Threading.Tasks;
using Content.Server.Preferences.Managers;
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
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly Dictionary<NetUserId, UserData> _users = new();
    private readonly List<OnLoadPlayer> _onLoadPlayer = [];
    private readonly List<OnFinishLoad> _onFinishLoad = [];
    private readonly List<OnPlayerDisconnect> _onPlayerDisconnect = [];

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

        foreach (var onDisconnect in _onPlayerDisconnect)
        {
            onDisconnect(session);
        }
    }

    private async Task Load(ICommonSession session, CancellationToken cancel)
    {
        // The task returned by this function is only ever observed by callers of WaitLoadComplete,
        // which doesn't even happen currently if the lobby is enabled.
        // As such, this task must NOT throw a non-cancellation error!
        try
        {
            var tasks = new List<Task>();
            foreach (var action in _onLoadPlayer)
            {
                tasks.Add(action(session, cancel));
            }

            await Task.WhenAll(tasks);

            cancel.ThrowIfCancellationRequested();

            foreach (var action in _onFinishLoad)
            {
                action(session);
            }

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

    public void AddOnLoadPlayer(OnLoadPlayer action)
    {
        _onLoadPlayer.Add(action);
    }

    public void AddOnFinishLoad(OnFinishLoad action)
    {
        _onFinishLoad.Add(action);
    }

    public void AddOnPlayerDisconnect(OnPlayerDisconnect action)
    {
        _onPlayerDisconnect.Add(action);
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("userdb");
    }

    private sealed record UserData(CancellationTokenSource Cancel, Task Task);

    public delegate Task OnLoadPlayer(ICommonSession player, CancellationToken cancel);

    public delegate void OnFinishLoad(ICommonSession player);

    public delegate void OnPlayerDisconnect(ICommonSession player);
}
