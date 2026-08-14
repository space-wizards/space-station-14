using Content.Shared.QuickDialog.BUI;
using Content.Shared.QuickDialog.Events;
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
        if (entries.Length == 0)
            throw new ArgumentException("Must specify at least one entry for the dialog!");

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
    /// <param name="ignoreOpen"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryOpenDialog(
        Enum uiKey,
        EntityUid target,
        EntityUid actor,
        string title,
        IQuickDialogEntry[] entries,
        QuickDialogButtonFlags buttons = QuickDialogButtonFlags.All,
        bool ignoreOpen = false)
    {
        if (entries.Length == 0)
            throw new ArgumentException("Must specify at least one entry for the dialog!");

        if (!ignoreOpen && _ui.IsUiOpen(target, uiKey))
            return false;

        if (!_ui.TryOpenUi(target, uiKey, actor, true))
            return false;

        var state = new QuickDialogOpenBoundUserInterfaceState(title, entries, buttons);
        _ui.SetUiState(target, uiKey, state);

        return true;
    }
}
