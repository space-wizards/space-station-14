using System.Threading;
using System.Threading.Tasks;
using Content.Server.Players.PlayTimeTracking;
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
public sealed class UserDbDataManager
{
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    private readonly Dictionary<NetUserId, UserData> _users = new();

    // TODO: Ideally connected/disconnected would be subscribed to IPlayerManager directly,
    // but this runs into ordering issues with game ticker.
    public void ClientConnected(ICommonSession session)
    {
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
        await Task.WhenAll(
            _prefs.LoadData(session, cancel),
            _playTimeTracking.LoadData(session, cancel));
    }

    public Task WaitLoadComplete(ICommonSession session)
    {
        return _users[session.UserId].Task;
    }

    public bool IsLoadComplete(ICommonSession session)
    {
        return GetLoadTask(session).IsCompleted;
    }

    public Task GetLoadTask(ICommonSession session)
    {
        return _users[session.UserId].Task;
    }

    private sealed record UserData(CancellationTokenSource Cancel, Task Task);
}
