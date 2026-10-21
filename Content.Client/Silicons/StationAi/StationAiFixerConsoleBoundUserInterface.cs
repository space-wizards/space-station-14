using Content.Shared.Silicons.StationAi;
using Robust.Client.UserInterface;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiFixerConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private StationAiFixerConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StationAiFixerConsoleWindow>();
        _window.SetOwner(Owner);

        _window.SendStationAiFixerConsoleMessageAction += SendStationAiFixerConsoleMessage;
        _window.OpenConfirmationDialogAction += OpenConfirmationDialog;
    }

    public override void Update()
    {
        base.Update();
        _window?.UpdateState();
    }

    private void OpenConfirmationDialog()
    {
        if (_window == null)
            return;

        _window.ConfirmationDialog?.Close();
        _window.ConfirmationDialog = new StationAiFixerConsoleConfirmationDialog();
        _window.ConfirmationDialog.OpenCentered();
        _window.ConfirmationDialog.SendStationAiFixerConsoleMessageAction += SendStationAiFixerConsoleMessage;
    }

    private void SendStationAiFixerConsoleMessage(StationAiFixerConsoleAction action)
    {
        SendPredictedMessage(new StationAiFixerConsoleMessage(action));
    }
}
