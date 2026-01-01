using Content.Client.UserInterface.Controls;
using Content.Shared.Administration;

namespace Content.Client.Administration;

/// <summary>
/// This handles the client portion of quick dialogs.
/// </summary>
public sealed class QuickDialogSystem : SharedQuickDialogSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<QuickDialogOpenEvent>(OpenDialog);
    }

    private int nextDialogId = 0;

    protected override int GetDialogId(Robust.Shared.Network.NetUserId userId, bool predicted)
    {
        var did = nextDialogId++;

        _mappingClientToLocal[(userId, did)] = did;
        return did;
    }

    private void OpenDialog(QuickDialogOpenEvent ev)
    {
        var ok = (ev.Buttons & QuickDialogButtonFlag.OkButton) != 0;
        var cancel = (ev.Buttons & QuickDialogButtonFlag.CancelButton) != 0;
        var window = new DialogWindow(ev.Title, ev.Prompts, ok: ok, cancel: cancel);

        if (ev.Predicted)
        {
            window.OnConfirmed += responses =>
            {
                RaisePredictiveEvent(new QuickDialogResponseEvent(ev.DialogId,
                    responses,
                    QuickDialogButtonFlag.OkButton));
            };

            window.OnCancelled += () =>
            {
                RaisePredictiveEvent(new QuickDialogResponseEvent(ev.DialogId,
                    new(),
                    QuickDialogButtonFlag.CancelButton));
            };
        }
        else
        {
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
}
