using Content.Shared._CD.Records;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using JetBrains.Annotations;

namespace Content.Client._CD.Records.UI;

[UsedImplicitly]
public sealed class CharacterRecordConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables] private CharacterRecordViewer? _window;

    [Dependency] private readonly EntityManager _entMan = default!;

    public CharacterRecordConsoleBoundUserInterface(EntityUid owner, Enum key)
        : base(owner, key)
    {
    }

    protected override void UpdateState(BoundUserInterfaceState baseState)
    {
        base.UpdateState(baseState);
        if (baseState is not CharacterRecordConsoleState state)
            return;

        if (_window?.IsSecurity() ?? false)
        {
            var comp = EntMan.GetComponent<CriminalRecordsConsoleComponent>(Owner);
            _window!.SecurityWantedStatusMaxLength = comp.MaxStringLength;
        }

        _window?.UpdateState(state);
    }

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OnClose += Close;
        _window.OnKeySelected += (ent, stationRecordKey) =>
        {
            SendMessage(new CharacterRecordConsoleSelectMsg(ent));

            // If we are a security records console, we also need to inform the criminal records
            // system of our state.
            if (_window.IsSecurity() && stationRecordKey != null)
            {
                SendMessage(new SelectStationRecord(stationRecordKey));
                _window.SetSecurityStatusEnabled(true);
            }
            else
            {
                // If the user does not have criminal records for some reason, we should not be able
                // to set their wanted status
                _window.SetSecurityStatusEnabled(false);
            }
        };

        _window.OnFiltersChanged += (ty, txt) =>
        {
            if (txt == null)
                SendMessage(new CharacterRecordsConsoleFilterMsg(null));
            else
                SendMessage(new CharacterRecordsConsoleFilterMsg(new StationRecordsFilter(ty, txt)));
        };

        _window.OnSetSecurityStatus += status =>
        {
            SendMessage(new CriminalRecordChangeStatus(status, null));
        };

        _window.OnSetWantedStatus += reason =>
        {
            SendMessage(new CriminalRecordChangeStatus(SecurityStatus.Wanted, reason));
        };

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
