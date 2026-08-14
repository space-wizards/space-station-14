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
        var window = new QuickDialogWindow(ev.Title, ev.Prompts, ev.Buttons);

        window.OnConfirmed += responses =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                QuickDialogButtonFlags.OkButton,
                responses));
        };

        window.OnCancelled += () =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                QuickDialogButtonFlags.CancelButton));
        };
    }
}
