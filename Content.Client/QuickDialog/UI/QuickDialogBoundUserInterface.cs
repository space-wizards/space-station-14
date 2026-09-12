using Content.Shared.QuickDialog;
using Content.Shared.QuickDialog.Messages;
using Robust.Client.UserInterface;

namespace Content.Client.QuickDialog.UI;

/// <summary>
///
/// </summary>
public sealed partial class QuickDialogBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private QuickDialogWindow? _window;

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<QuickDialogWindow>();

        _window.OnConfirmed += (responses) => OnResponse(QuickDialogButtonFlags.OkButton, responses);
        _window.OnCancelled += () => OnResponse(QuickDialogButtonFlags.CancelButton);
    }

    /// <inheritdoc/>
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is not QuickDialogOpenMessage msg)
            return;

        _window?.Update(msg.Title, msg.Entries, msg.Buttons);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="responses"></param>
    private void OnResponse(QuickDialogButtonFlags button, string[]? responses = null)
    {
        var message = new QuickDialogResponseMessage(button, responses);
        SendPredictedMessage(message);
    }
}
