using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Client.GameObjects;

namespace Content.Client.CriminalRecords;

public sealed class GeneralCriminalRecordConsoleBoundUserInterface : BoundUserInterface
{
    private GeneralCriminalRecordConsoleWindow? _window = default!;

    public GeneralCriminalRecordConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {}

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OnKeySelected += OnKeySelected;
        _window.OnClose += Close;

        _window.OpenCentered();

        _window.OnArrestButtonPressed += (_, reason, name) => SendMessage(new CriminalRecordArrestButtonPressed(reason, name));
        _window.OnStatusOptionButtonSelected += (_, status, reason, name) => SendMessage(new CriminalStatusOptionButtonSelected(status, reason, name));
    }

    private void OnKeySelected(StationRecordKey? key)
    {
        SendMessage(new SelectGeneralCriminalRecord(key));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GeneralCriminalRecordConsoleState cast)
        {
            return;
        }

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
