using Content.Client.QuickDialog.UI;
using Content.Shared.QuickDialog;
using Content.Shared.QuickDialog.Events;

namespace Content.Client.Administration;

/// <inheritdoc/>
public sealed partial class ClientQuickDialogSystem : QuickDialogSystem
{
    [SubscribeNetworkEvent]
    private void OpenDialog(QuickDialogOpenEvent ev)
    {
        var window = new QuickDialogWindow();
        window.Update(ev.Title, ev.Entries, ev.Buttons);

        window.OnConfirmed += (responses) => OnResponse(ev.DialogId, QuickDialogButtonFlags.OkButton, responses);
        window.OnCancelled += () => OnResponse(ev.DialogId, QuickDialogButtonFlags.CancelButton);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="responses"></param>
    private void OnResponse(string dialogId, QuickDialogButtonFlags button, string[]? responses = null)
    {
        RaiseNetworkEvent(new QuickDialogResponseEvent(dialogId,
            button,
            responses));
    }
}
