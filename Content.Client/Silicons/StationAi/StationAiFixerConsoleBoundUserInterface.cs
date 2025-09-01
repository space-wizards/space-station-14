using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiFixerConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private StationAiFixerConsoleWindow? _window;
    private StationAiFixerConsoleConfirmationDialog? _confirmationDialog;

    protected override void Open()
    {
        base.Open();

        _window = new StationAiFixerConsoleWindow(Owner);
        _window.OpenCentered();
        _window.OnClose += Close;

        _window.SendStationAiFixerConsoleMessageAction += SendStationAiFixerConsoleMessage;
        _window.OpenConfirmationDialogAction += OpenConfirmationDialog;
    }

    private void SendStationAiFixerConsoleMessage(StationAiFixerConsoleAction action)
    {
        SendPredictedMessage(new StationAiFixerConsoleMessage(action));
    }

    public override void Update()
    {
        base.Update();
        _window?.UpdateState();
    }

    private void OpenConfirmationDialog()
    {
        _confirmationDialog?.Close();

        _confirmationDialog = new StationAiFixerConsoleConfirmationDialog();
        _confirmationDialog.OpenCentered();

        _confirmationDialog.SendStationAiFixerConsoleMessageAction += SendStationAiFixerConsoleMessage;
    }

    protected override void Dispose(bool disposing)
    {
        _window = null;
        _confirmationDialog = null;
    }
}
