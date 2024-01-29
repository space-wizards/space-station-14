using Content.Client.UserInterface.Controls;
using Content.Shared.Administration;
using Content.Shared.CriminalRecords;
using Content.Shared.Dataset;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.CriminalRecords;

public sealed class CriminalRecordsConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [ValidatePrototypeId<DatasetPrototype>]
    private const string ReasonPlaceholders = "CriminalRecordsWantedReasonPlaceholders";

    private CriminalRecordsConsoleWindow? _window;
    private CrimeHistoryWindow? _historyWindow;
    private DialogWindow? _reasonDialog;

    public CriminalRecordsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

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
            GetWantedReason();
            return;
        }

        SendMessage(new CriminalRecordChangeStatus(status, null));
    }

    private void GetWantedReason()
    {
        if (_reasonDialog != null)
        {
            _reasonDialog.MoveToFront();
            return;
        }

        var field = "reason";
        var title = Loc.GetString("criminal-records-status-wanted");
        var placeholders = _proto.Index<DatasetPrototype>(ReasonPlaceholders);
        var placeholder = _random.Pick(placeholders.Values); // just funny it doesn't actually get used
        var prompt = Loc.GetString("criminal-records-console-reason");
        var entry = new QuickDialogEntry(field, QuickDialogEntryType.LongText, prompt, placeholder);
        var entries = new List<QuickDialogEntry>() { entry };
        _reasonDialog = new DialogWindow(title, entries);

        _reasonDialog.OnConfirmed += responses =>
        {
            var reason = responses[field];
            // TODO: same as history unhardcode
            if (reason.Length < 1 || reason.Length > 256)
                return;

            SendMessage(new CriminalRecordChangeStatus(SecurityStatus.Wanted, reason));
        };

        _reasonDialog.OnClose += () => { _reasonDialog = null; };
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
        _reasonDialog?.Close();
    }
}
