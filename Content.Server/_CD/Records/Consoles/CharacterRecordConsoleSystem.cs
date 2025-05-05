using Content.Server.CriminalRecords.Systems; // DeltaV - i hate this, forward to criminal records console
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Server.StationRecords;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Components; // DeltaV - i hate this, forward to criminal records console
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using Robust.Server.GameObjects;

namespace Content.Server._CD.Records.Consoles;

public sealed class CharacterRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly CharacterRecordsSystem _characterRecords = default!;
    [Dependency] private readonly CriminalRecordsConsoleSystem _criminalRecordsConsole = default!; // DeltaV - i hate this, forward to criminal records console
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterRecordConsoleComponent, CharacterRecordsModifiedEvent>((uid, component, _) =>
            UpdateUi(uid, component));

        Subs.BuiEvents<CharacterRecordConsoleComponent>(CharacterRecordConsoleKey.Key,
            subr =>
            {
                subr.Event<BoundUIOpenedEvent>((uid, component, _) => UpdateUi(uid, component));
                subr.Event<CharacterRecordConsoleSelectMsg>(OnKeySelect);
                subr.Event<CharacterRecordsConsoleFilterMsg>(OnFilterApplied);
                // Begin DeltaV - i hate this, forward to criminal records console
                subr.Event<SelectStationRecord>(OnSelectStationRecord);
                subr.Event<CriminalRecordChangeStatus>(OnCriminalRecordChangeStatus);
                // End DeltaV - i hate this, forward to criminal records console
            });
    }

    private void OnFilterApplied(Entity<CharacterRecordConsoleComponent> ent, ref CharacterRecordsConsoleFilterMsg msg)
    {
        ent.Comp.Filter = msg.Filter;
        UpdateUi(ent);
    }

    private void OnKeySelect(Entity<CharacterRecordConsoleComponent> ent, ref CharacterRecordConsoleSelectMsg msg)
    {
        ent.Comp.SelectedIndex = msg.CharacterRecordKey;
        UpdateUi(ent);
    }

    // Begin DeltaV - i hate this, forward to criminal records console
    private void OnSelectStationRecord(Entity<CharacterRecordConsoleComponent> ent, ref SelectStationRecord msg)
    {
        if (!TryComp<CriminalRecordsConsoleComponent>(ent, out var console))
            return;

        _criminalRecordsConsole.OnKeySelected((ent.Owner, console), ref msg);
        UpdateUi(ent);
    }

    private void OnCriminalRecordChangeStatus(Entity<CharacterRecordConsoleComponent> ent, ref CriminalRecordChangeStatus msg)
    {
        if (!TryComp<CriminalRecordsConsoleComponent>(ent, out var console))
            return;

        _criminalRecordsConsole.OnChangeStatus((ent.Owner, console), ref msg);
        UpdateUi(ent);
    }
    // End DeltaV - i hate this, forward to criminal records console

    private void UpdateUi(EntityUid entity, CharacterRecordConsoleComponent? console = null)
    {
        if (!Resolve(entity, ref console))
            return;

        var station = _station.GetOwningStation(entity);
        if (!HasComp<StationRecordsComponent>(station) ||
            !HasComp<CharacterRecordsComponent>(station))
        {
            SendState(entity, new CharacterRecordConsoleState { ConsoleType = console.ConsoleType });
            return;
        }

        var characterRecords = _characterRecords.QueryRecords(station.Value);
        // Get the name and station records key display from the list of records
        var names = new Dictionary<uint, CharacterRecordConsoleState.CharacterInfo>();
        foreach (var (i, r) in characterRecords)
        {
            var netEnt = _entity.GetNetEntity(r.Owner!.Value);
            // Admins get additional info to make it easier to run commands
            var nameJob = console.ConsoleType != RecordConsoleType.Admin
                ? $"{r.Name} ({r.JobTitle})"
                : $"{r.Name} ({netEnt}, {r.JobTitle}";

            // Apply any filter the user has set
            if (console.Filter != null)
            {
                if (IsSkippedRecord(console.Filter, r, nameJob))
                    continue;
            }

            if (names.ContainsKey(i))
            {
                Log.Error(
                    $"We somehow have duplicate character record keys, NetEntity: {i}, Entity: {entity}, Character Name: {r.Name}");
            }

            names[i] = new CharacterRecordConsoleState.CharacterInfo
                { CharacterDisplayName = nameJob, StationRecordKey = r.StationRecordsKey };
        }

        var record =
            console.SelectedIndex == null || !characterRecords.TryGetValue(console.SelectedIndex!.Value, out var value)
                ? null
                : value;
        (SecurityStatus, string?)? securityStatus = null;

        // If we need the character's security status, gather it from the criminal records
        if ((console.ConsoleType == RecordConsoleType.Admin ||
             console.ConsoleType == RecordConsoleType.Security)
            && record?.StationRecordsKey != null)
        {
            var key = new StationRecordKey(record.StationRecordsKey.Value, station.Value);
            if (_records.TryGetRecord<CriminalRecord>(key, out var entry))
                securityStatus = (entry.Status, entry.Reason);
        }

        SendState(entity,
            new CharacterRecordConsoleState
            {
                ConsoleType = console.ConsoleType,
                CharacterList = names,
                SelectedIndex = console.SelectedIndex,
                SelectedRecord = record,
                Filter = console.Filter,
                SelectedSecurityStatus = securityStatus,
            });
    }

    private void SendState(EntityUid entity, CharacterRecordConsoleState state)
    {
        _ui.SetUiState(entity, CharacterRecordConsoleKey.Key, state);
    }

    /// <summary>
    /// Almost exactly the same as <see cref="StationRecordsSystem.IsSkipped"/>
    /// </summary>
    private static bool IsSkippedRecord(StationRecordsFilter filter,
        FullCharacterRecords record,
        string nameJob)
    {
        var isFilter = filter.Value.Length > 0;

        if (!isFilter)
            return false;

        var filterLowerCaseValue = filter.Value.ToLower();

        return filter.Type switch
        {
            StationRecordFilterType.Name =>
                !nameJob.Contains(filterLowerCaseValue, StringComparison.CurrentCultureIgnoreCase),
            StationRecordFilterType.Prints => record.Fingerprint != null
                && IsFilterWithSomeCodeValue(record.Fingerprint, filterLowerCaseValue),
            StationRecordFilterType.DNA => record.DNA != null
                                                && IsFilterWithSomeCodeValue(record.DNA, filterLowerCaseValue),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid Character Record filter type"),
        };
    }

    private static bool IsFilterWithSomeCodeValue(string value, string filter)
    {
        return !value.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase);
    }
}
