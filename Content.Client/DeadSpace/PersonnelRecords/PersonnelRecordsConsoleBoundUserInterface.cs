// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.PersonnelRecords;
using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.StationRecords;

namespace Content.Client.DeadSpace.PersonnelRecords;

public sealed class PersonnelRecordsConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PersonnelRecordsConsoleWindow? _window;

    [ViewVariables]
    private PersonnelHistoryWindow? _historyWindow;

    public PersonnelRecordsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var comp = EntMan.GetComponent<PersonnelRecordsConsoleComponent>(Owner);

        _window = new(comp.MaxStringLength);
        _window.OnKeySelected += key =>
            SendMessage(new SelectStationRecord(key));
        _window.OnFiltersChanged += (type, value) =>
            SendMessage(new SetStationRecordFilter(type, value));
        _window.OnStatusFilterPressed += status =>
            SendMessage(new PersonnelRecordSetStatusFilter(status));
        _window.OnIssueOrder += (status, reason) =>
            SendMessage(new PersonnelRecordIssueOrder(status, reason));
        _window.OnAnnulOrder += reason =>
            SendMessage(new PersonnelRecordAnnulOrder(reason));
        _window.OnDeclareWanted += reason =>
            SendMessage(new PersonnelRecordDeclareWanted(reason));
        _window.OnPrintOrder += () =>
            SendMessage(new PersonnelRecordPrintOrder());
        _window.OnHistoryUpdated += UpdateHistory;
        _window.OnHistoryClosed += () => _historyWindow?.Close();
        _window.OnClose += Close;

        _historyWindow = new();
        _historyWindow.Close(); // leave closed until the user opens it
    }

    /// <summary>
    /// Updates or opens the (read-only) order history window.
    /// </summary>
    private void UpdateHistory(PersonnelRecord record, bool open)
    {
        _historyWindow!.UpdateHistory(record);

        if (open)
            _historyWindow.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not PersonnelRecordsConsoleState cast)
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
