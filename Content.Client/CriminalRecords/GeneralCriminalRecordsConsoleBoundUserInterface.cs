using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Client.GameObjects;

namespace Content.Client.CriminalRecords;

public sealed class GeneralCriminalRecordsConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
	private GeneralCriminalRecordsConsoleWindow? _window = default!;

    public GeneralCriminalRecordsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
	}

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OnKeySelected += OnKeySelected;
        _window.OnFiltersChanged += OnFiltersChanged;
        _window.OnClose += Close;
		_window.OnArrestButtonPressed += (_, reason, name) => SendMessage(new CriminalRecordArrestButtonPressed(reason, name));
		_window.OnReleaseButtonPressed += (_, reason, name) => SendMessage(new CriminalRecordReleaseButtonPressed(reason, name));
		_window.OnWantedButtonPressed += (_, reason, name) => SendMessage(new CriminalRecordWantedButtonPressed(reason, name));
    
        _window.OpenCentered();
        }

    private void OnKeySelected((NetEntity, uint)? key)
    {
        SendMessage(new SelectGeneralCriminalRecord(key));
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

        if (state is not GeneralCriminalRecordsConsoleState cast)
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