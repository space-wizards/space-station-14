using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Players.Whitelist;

/// <summary>
/// Managed for caching whitelist statuses for connected players on the server.
/// If trying to get the data of disconnected players, use <see cref="IServerDbManager.GetWhitelistStatusAsync"/> directly.
/// </summary>
public sealed partial class WhitelistManager : IPostInjectInit
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private UserDbDataManager _userDb = default!;
    [Dependency] private ILogManager _logManager = default!;

    private readonly Dictionary<NetUserId, bool> _whitelistStatus = new();
    private ISawmill _sawmill = default!;

    private async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        var whitelistStatus = await _db.GetWhitelistStatusAsync(session.UserId);
        _whitelistStatus[session.UserId] = whitelistStatus;
    }

    private void ClientDisconnected(ICommonSession session)
    {
        _whitelistStatus.Remove(session.UserId);
    }

    /// <inheritdoc cref="IsConnectedWhitelisted(Robust.Shared.Network.NetUserId)"/>
    public bool IsConnectedWhitelisted(ICommonSession session)
    {
        return IsConnectedWhitelisted(session.UserId);
    }

    /// <summary>
    /// Returns true if the player has whitelist status on the server, false if they don't.
    /// Returns false and raises an error if the player is not in the cached database, meaning they are either not connected or database failed.
    /// </summary>
    public bool IsConnectedWhitelisted(NetUserId userId)
    {
        if (!_whitelistStatus.TryGetValue(userId, out var whitelistStatus))
        {
            _sawmill.Error("{Player} is either not connected, or the database failed to load their whitelist status. Stack trace:\\n{StackTrace}",
                userId,
                Environment.StackTrace);
            return false;
        }

        return whitelistStatus;
    }

    /// <summary>
    /// Adds a player to the whitelist and updates the tracker.
    /// </summary>
    public async void AddWhitelist(NetUserId player)
    {
        if (_whitelistStatus.ContainsKey(player))
            _whitelistStatus[player] = true;

        await _db.AddToWhitelistAsync(player);
    }

    /// <summary>
    /// Removes a player from the whitelist and updates the tracker.
    /// </summary>
    public async void RemoveWhitelist(NetUserId player)
    {
        if (_whitelistStatus.ContainsKey(player))
            _whitelistStatus[player] = false;

        await _db.RemoveFromWhitelistAsync(player);
    }

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
        _sawmill = _logManager.GetSawmill("server_whitelist");
    }
}
