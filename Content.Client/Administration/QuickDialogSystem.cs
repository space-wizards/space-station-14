using Content.Client.UserInterface.Controls;
using Content.Shared.Administration;

namespace Content.Client.Administration;

/// <summary>
/// This handles the client portion of quick dialogs.
/// </summary>
public sealed class QuickDialogSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<QuickDialogOpenEvent>(OpenDialog);
    }

    private void OpenDialog(QuickDialogOpenEvent ev)
    {
        var ok = (ev.Buttons & QuickDialogButtonFlag.OkButton) != 0;
        var cancel = (ev.Buttons & QuickDialogButtonFlag.CancelButton) != 0;
        var window = new DialogWindow(ev.Title, ev.Prompts, ok: ok, cancel: cancel);

        window.OnConfirmed += responses =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                responses,
                QuickDialogButtonFlag.OkButton));
        };

        window.OnCancelled += () =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                new(),
                QuickDialogButtonFlag.CancelButton));
        };
    }
}
