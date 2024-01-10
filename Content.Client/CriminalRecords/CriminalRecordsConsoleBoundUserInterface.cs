using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;

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
        _window.OnAddHistoryPressed += (_, line) => SendMessage(new CriminalRecordAddHistory(line));
        _window.OnDeleteHistoryPressed += (_, index) => SendMessage(new CriminalRecordDeleteHistory(index));
    }

    // TODO: this can easily be shared with a client records console system
    // same with adding the ui elements themselves
    private void OnKeySelected(uint? key)
    {
        SendMessage(new SelectStationRecord(key));
    }

    private void OnFiltersChanged(
        StationRecordFilterType type, string filterValue)
    {
        SendMessage(new SetStationRecordFilter(type, filterValue));
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
