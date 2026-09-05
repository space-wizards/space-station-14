using Content.Shared.QuickDialog.Events;
using Content.Shared.QuickDialog.Messages;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Shared.QuickDialog;

public abstract partial class QuickDialogSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    /// <summary>
    ///
    /// </summary>
    /// <param name="uniqueId"></param>
    /// <param name="session"></param>
    /// <param name="title"></param>
    /// <param name="entries"></param>
    /// <param name="okAction"></param>
    /// <param name="cancelAction"></param>
    /// <param name="buttons"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryOpenDialog(
        string uniqueId,
        ICommonSession session,
        string title,
        IQuickDialogEntry[] entries,
        Action<object[]> okAction,
        Action? cancelAction = null,
        QuickDialogButtonFlags buttons = QuickDialogButtonFlags.All)
    {
        if (!_openDialogsPerUser.TryGetValue(session.UserId, out var dialogs))
        {
            dialogs = [];
            _openDialogsPerUser.Add(session.UserId, dialogs);
        }
        else if (dialogs.ContainsKey(uniqueId))
        {
            return false;
        }

        dialogs.Add(uniqueId, (entries, okAction, cancelAction));

        RaiseNetworkEvent(
            new QuickDialogOpenEvent(
                uniqueId,
                title,
                entries,
                buttons),
            session
        );

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="uiKey"></param>
    /// <param name="target"></param>
    /// <param name="actor"></param>
    /// <param name="title"></param>
    /// <param name="entries"></param>
    /// <param name="buttons"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryOpenDialog(
        Enum uiKey,
        EntityUid target,
        EntityUid actor,
        string title,
        IQuickDialogEntry[] entries,
        Action<object[]> okAction,
        Action? cancelAction = null,
        QuickDialogButtonFlags buttons = QuickDialogButtonFlags.All,
        bool predicted = false)
    {
        if (!_playerManager.TryGetSessionByEntity(actor, out var session))
            return false;

        var netEntity = GetNetEntity(target);
        if (!_openBUIDialogsPerUser.TryGetValue(session.UserId, out var dialogs))
        {
            dialogs = [];
            _openBUIDialogsPerUser.Add(session.UserId, dialogs);
        }
        else if (dialogs.ContainsKey((netEntity, uiKey)))
        {
            return false;
        }

        if (!_ui.TryOpenUi(target, uiKey, actor, true))
            return false;

        if (!_ui.TryGetOpenUi(target, uiKey, out var bui))
            return false;

        var message = new QuickDialogS(title, entries, buttons);

        if (predicted)
            _ui.SendPredictedUiMessage(target, bui, message);
        else
            _ui.ServerSendUiMessage(target, bui, message);

        dialogs.Add((netEntity, uiKey), (entries, okAction, cancelAction));

        return true;
    }
}
