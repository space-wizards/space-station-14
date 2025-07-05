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
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;

        SubscribeNetworkEvent<QuickDialogResponseEvent>(Handler);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
    }

    /// <summary>
    /// Contains the success/cancel actions for a dialog.
    /// </summary>
    private readonly Dictionary<int, Dialog> _openDialogs = new();
    private readonly Dictionary<NetUserId, List<int>> _openDialogsByUser = new();
    private int _nextServerDialogId = 0;
    private Dictionary<NetUserId, int> _nextClientDialogId = new();
    private readonly Dictionary<(NetUserId, int), int> _mappingServerToClient = new();
    private readonly Dictionary<(NetUserId, int), int> _mappingClientToServer = new();

    protected override int GetDialogId(NetUserId userId)
    {
        if (!_nextClientDialogId.ContainsKey(userId))
            _nextClientDialogId[userId] = 0;

        var didClient = _nextClientDialogId[userId]++;
        var didServer = _nextServerDialogId++;

        _mappingServerToClient[(userId, didServer)] = didClient;
        _mappingClientToServer[(userId, didClient)] = didServer;

        return didServer;
    }

    protected override int OpenDialogInternal(ICommonSession session, string title, List<QuickDialogEntry> entries, QuickDialogButtonFlag buttons, Action<QuickDialogResponseEvent> okAction, Action cancelAction)
    {
        var did = base.OpenDialogInternal(session, title, entries, buttons, okAction, cancelAction);

        _openDialogs.Add(did, new Dialog(okAction, cancelAction));
        if (!_openDialogsByUser.ContainsKey(session.UserId))
            _openDialogsByUser.Add(session.UserId, new());

        _openDialogsByUser[session.UserId].Add(did);

        return did;
    }

    private void Handler(QuickDialogResponseEvent msg, EntitySessionEventArgs args)
    {
        if (!_mappingClientToServer.ContainsKey((args.SenderSession.UserId, msg.DialogId)))
        {
            args.SenderSession.Channel.Disconnect($"Replied with invalid quick dialog data with id {msg.DialogId} for {args.SenderSession.UserId}.");
            return;
        }

        var didServer = _mappingClientToServer[(args.SenderSession.UserId, msg.DialogId)];

        if (!_openDialogs.ContainsKey(didServer) || !_openDialogsByUser[args.SenderSession.UserId].Contains(didServer))
        {
            args.SenderSession.Channel.Disconnect($"Replied with invalid quick dialog data with id {msg.DialogId}({didServer}).");
            return;
        }

        switch (msg.ButtonPressed)
        {
            case QuickDialogButtonFlag.OkButton:
                _openDialogs[didServer].okAction.Invoke(msg);
                break;
            case QuickDialogButtonFlag.CancelButton:
                _openDialogs[didServer].cancelAction.Invoke();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _openDialogs.Remove(didServer);
        _openDialogsByUser[args.SenderSession.UserId].Remove(didServer);
        _mappingClientToServer.Remove((args.SenderSession.UserId, msg.DialogId));
        _mappingServerToClient.Remove((args.SenderSession.UserId, didServer));
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Disconnected && e.NewStatus != SessionStatus.Zombie)
            return;

        var user = e.Session.UserId;

        if (!_openDialogsByUser.ContainsKey(user))
            return;

        foreach (var dialogId in _openDialogsByUser[user])
        {
            _openDialogs[dialogId].cancelAction.Invoke();
            _openDialogs.Remove(dialogId);
        }

        _openDialogsByUser.Remove(user);
    }
}
