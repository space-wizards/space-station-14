using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.CriminalRecords;

public sealed class CriminalRecordsConsoleBoundUserInterface : BoundUserInterface
{
    private CriminalRecordsConsoleWindow? _window;

    public CriminalRecordsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {}

    protected override void Open()
    {
        base.Open();

        _window = new(Owner);
        _window.OnKeySelected += OnKeySelected;
        _window.OnFiltersChanged += OnFiltersChanged;
        _window.OnClose += Close;

        _window.OpenCentered();

        _window.OnArrestButtonPressed += (_, reason, name) => SendMessage(new CriminalRecordArrestButtonPressed(reason, name));
        _window.OnStatusOptionButtonSelected += (_, status, reason, name) => SendMessage(new CriminalStatusOptionButtonSelected(status, reason, name));
    }

    private void OnKeySelected(uint? key)
    {
        SendMessage(new SelectCriminalRecords(key));
    }

    private void OnFiltersChanged(
        GeneralStationRecordFilterType type, string filterValue)
    {
        GeneralStationRecordsFilterMsg msg = new(type, filterValue);
        SendMessage(msg);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CriminalRecordsConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
