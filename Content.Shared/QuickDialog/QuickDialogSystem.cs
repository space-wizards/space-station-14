using Content.Shared.QuickDialog.Events;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.QuickDialog;

/// <summary>
///
/// </summary>
public abstract partial class QuickDialogSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _playerManager = default!;

    /// <summary>
    ///
    /// </summary>
    private readonly Dictionary<NetUserId, Dictionary<string, (Action<QuickDialogResponseEvent> okAction, Action? cancelAction)>> _openDialogsPerUser = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
    }

    /// <inheritdoc/>
    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Disconnected && e.NewStatus != SessionStatus.Zombie)
            return;

        foreach (var (_, actions) in _openDialogsPerUser[e.Session.UserId])
        {
            actions.cancelAction?.Invoke();
        }

        _openDialogsPerUser.Remove(e.Session.UserId);
    }

    [SubscribeNetworkEvent]
    private void Handler(QuickDialogResponseEvent msg, EntitySessionEventArgs args)
    {
        if (!_openDialogsPerUser.TryGetValue(args.SenderSession.UserId, out var dialogs) || !dialogs.TryGetValue(msg.DialogId, out var actions))
        {
            args.SenderSession.Channel.Disconnect($"Replied with invalid quick dialog data with id {msg.DialogId}.");
            return;
        }

        switch (msg.ButtonPressed)
        {
            case QuickDialogButtonFlag.OkButton:
                actions.okAction.Invoke(msg);
                break;
            case QuickDialogButtonFlag.CancelButton:
                actions.cancelAction?.Invoke();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(msg), nameof(msg.ButtonPressed) + ": Invalid button flag.");
        }

        dialogs.Remove(msg.DialogId);
    }
}

/// <summary>
/// The buttons available in a quick dialog.
/// </summary>
[Flags]
public enum QuickDialogButtonFlag : byte
{
    /// <summary>
    ///
    /// </summary>
    OkButton,

    /// <summary>
    ///
    /// </summary>
    CancelButton,
}
