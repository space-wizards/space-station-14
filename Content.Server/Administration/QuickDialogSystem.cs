using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration;

/// <summary>
/// This handles the server portion of quick dialogs, including opening them.
/// </summary>
public sealed partial class QuickDialogSystem : SharedQuickDialogSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
    }

    /// <summary>
    /// Contains the success/cancel actions for a dialog.
    /// </summary>
    private int _nextServerDialogId = 0;
    private readonly Dictionary<NetUserId, int> _clientNextDialogId = new();

    protected override int GetDialogId(NetUserId userId, bool predicted)
    {
        int didClient;
        var didServer = _nextServerDialogId++;

        if (!_clientNextDialogId.ContainsKey(userId))
            _clientNextDialogId[userId] = 0;

        if (predicted)
            didClient = _clientNextDialogId[userId]++;
        else
            didClient = didServer;

        _mappingClientToLocal[(userId, didClient)] = didServer;

        return didServer;
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Disconnected && e.NewStatus != SessionStatus.Zombie)
            return;

        var user = e.Session.UserId;
        var oldDialogs = new List<(NetUserId, int)>();

        foreach (var key in _mappingClientToLocal.Keys)
        {
            if (user != key.Item1)
                continue;

            oldDialogs.Add(key);
        }

        foreach (var key in oldDialogs)
        {
            _mappingClientToLocal.Remove(key);
        }

        _clientNextDialogId.Remove(user);
    }
}
