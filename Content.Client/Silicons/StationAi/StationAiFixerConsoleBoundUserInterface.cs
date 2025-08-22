using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiFixerConsoleBoundUserInterface : BoundUserInterface
{
    private StationAiFixerConsoleWindow? _window;
    private StationAiFixerConsoleConfirmationDialog? _confirmationDialog;

    public StationAiFixerConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    public override void Update()
    {
        base.Update();
        _window?.UpdateState();
    }

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

    private void OpenConfirmationDialog()
    {
        if (_confirmationDialog != null)
            _confirmationDialog.Close();

        _confirmationDialog = new StationAiFixerConsoleConfirmationDialog(Owner);
        _confirmationDialog.OpenCentered();

        _confirmationDialog.SendStationAiFixerConsoleMessageAction += SendStationAiFixerConsoleMessage;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Dispose();
        _confirmationDialog?.Dispose();
    }
}
