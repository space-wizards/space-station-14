using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Players.Whitelist;

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

    /// <inheritdoc cref="IsWhitelisted(Robust.Shared.Network.NetUserId)"/>
    public bool IsWhitelisted(ICommonSession session)
    {
        return IsWhitelisted(session.UserId);
    }

    /// <summary>
    /// Returns true if the player has whitelist status on the server.
    /// Returns false if the player doesn't, or if the database failed to load them.
    /// </summary>
    public bool IsWhitelisted(NetUserId userId)
    {
        if (!_whitelistStatus.TryGetValue(userId, out var whitelistStatus))
        {
            _sawmill.Error("Unable to check if player {Player} is whitelisted for the server in the database. Stack trace:\\n{StackTrace}",
                userId,
                Environment.StackTrace);
            return false;
        }

        return whitelistStatus;
    }

    /// <summary>
    /// Adds a player to the whitelist and the whitelist tracker.
    /// </summary>
    public async void AddWhitelist(NetUserId player)
    {
        _whitelistStatus[player] = true;

        await _db.AddToWhitelistAsync(player);
    }

    /// <summary>
    /// Removes a player from the whitelist and the whitelist tracker.
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
