using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;

namespace Content.Client.CriminalRecords;

public sealed class CriminalRecordsConsoleBoundUserInterface : BoundUserInterface
{
    private CriminalRecordsConsoleWindow? _window;
    private CrimeHistoryWindow? _historyWindow;

    public CriminalRecordsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {}

    protected override void Open()
    {
        base.Open();

        _window = new(Owner);
        _window.OnKeySelected += OnKeySelected;
        _window.OnFiltersChanged += OnFiltersChanged;
        _window.OnStatusSelected += SetStatus;
        _window.OnHistoryUpdated += UpdateHistory;
        _window.OnHistoryClosed += () => _historyWindow?.Close();
        _window.OnClose += Close;

        _window.OpenCentered();

        _historyWindow = new();
        _historyWindow.OnAddHistory += line => SendMessage(new CriminalRecordAddHistory(line));
        _historyWindow.OnDeleteHistory += index => SendMessage(new CriminalRecordDeleteHistory(index));

        _historyWindow.Close(); // leave closed until user opens it
    }

    // TODO: this can easily be shared with a client records console system
    // same with adding the ui elements themselves, could be put in a control
    private void OnKeySelected(uint? key)
    {
        SendMessage(new SelectStationRecord(key));
    }

    private void OnFiltersChanged(
        StationRecordFilterType type, string filterValue)
    {
        SendMessage(new SetStationRecordFilter(type, filterValue));
    }

    private void SetStatus(SecurityStatus status)
    {
        if (status == SecurityStatus.Wanted)
        {
            // TODO: open dialog
            return;
        }

        SendMessage(new CriminalRecordChangeStatus(status, null));
    }

    /// <summary>
    /// Updates or opens a new history window.
    /// </summary>
    private void UpdateHistory(CriminalRecord record, bool access, bool open)
    {
        _historyWindow!.UpdateHistory(record, access);

        if (open)
            _historyWindow.OpenCentered();
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
        _historyWindow?.Close();
    }
}
