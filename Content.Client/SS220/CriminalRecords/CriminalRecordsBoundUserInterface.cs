// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.CriminalRecords.UI;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.CriminalRecords;

public sealed class CriminalRecordsBoundUserInterface : BoundUserInterface
{
    CriminalRecordsWindow? _window;

    public CriminalRecordsBoundUserInterface(EntityUid owner, Enum key) : base(owner, key)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OnClose += Close;
        _window.OnKeySelected += OnKeySelected;
        _window.OnCriminalStatusChange += OnStatusUpdated;
        _window.OnCriminalStatusDelete += OnStatusDeleted;

        if (EntMan.TryGetComponent<CriminalRecordsConsoleComponent>(Owner, out var comp))
        {
            _window.SetSecurityMode(comp.IsSecurity);
            _window.MaxEntryMessageLength = comp.MaxMessageLength;
            _window.EditCooldown = (int) comp.EditCooldown.TotalSeconds;
        }

        _window.OpenCentered();
        if (EntMan.TryGetComponent<MetaDataComponent>(Owner, out var metaData))
            _window.Title = metaData.EntityName;
    }

    private void OnKeySelected((NetEntity, uint)? key)
    {
        SendMessage(new SelectGeneralStationRecord(key));
    }

    private void OnStatusUpdated((string, ProtoId<CriminalStatusPrototype>?) statusUpdate)
    {
        SendMessage(new UpdateCriminalRecordStatus(statusUpdate.Item1, statusUpdate.Item2));
    }
    private void OnStatusDeleted(int time)
    {
        SendMessage(new DeleteCriminalRecordStatus(time));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CriminalRecordConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
