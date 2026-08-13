using Content.Shared.QuickDialog.BUI;
using Content.Shared.QuickDialog.Events;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Shared.QuickDialog;

public abstract partial class QuickDialogSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="uniqueId"></param>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="entries"></param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <param name="buttons"></param>
    /// <returns></returns>
    [PublicAPI]
    public bool TryOpenDialog(
        string uniqueId,
        ICommonSession session,
        string title,
        IQuickDialogEntry[] entries,
        Action<object?[]> okAction,
        Action? cancelAction = null,
        QuickDialogButtonFlag buttons = QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton)
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

        dialogs.Add(uniqueId, (ev =>
        {
            if (ev.Responses == null || ev.Responses.Length != entries.Length)
            {
                session.Channel.Disconnect("Replied with invalid quick dialog data.");
                cancelAction?.Invoke();
                return;
            }

            var answers = new object?[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (!entry.TryParse(ev.Responses[i], out var answer))
                {
                    if (entry.Required)
                    {
                        session.Channel.Disconnect("Replied with invalid quick dialog data.");
                        cancelAction?.Invoke();
                        return;
                    }

                    continue;
                }

                answers[i] = answer;
            }

            okAction.Invoke(answers);
        }, cancelAction));

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
    /// <param name="actor">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
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
        QuickDialogButtonFlag buttons = QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
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
