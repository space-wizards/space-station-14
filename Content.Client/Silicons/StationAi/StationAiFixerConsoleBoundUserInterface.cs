using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiFixerConsoleBoundUserInterface : BoundUserInterface
{
    private StationAiFixerConsoleWindow? _window;

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
    }

    public void SendStationAiFixerConsoleMessage(StationAiFixerConsoleAction action)
    {
        SendPredictedMessage(new StationAiFixerConsoleMessage(action));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Dispose();
    }
}
