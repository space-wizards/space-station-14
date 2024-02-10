using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared._CD.Records;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;

namespace Content.Server._CD.Records.Consoles;

public sealed class CharacterRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly CharacterRecordsSystem _characterRecords = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterRecordConsoleComponent, CharacterRecordsModifiedEvent>((uid, component, _) => UpdateUi(uid, component));

        Subs.BuiEvents<CharacterRecordConsoleComponent>(CharacterRecordConsoleKey.Key, subr =>
        {
            subr.Event<BoundUIOpenedEvent>((uid, component, _) => UpdateUi(uid, component));
            subr.Event<CharacterRecordConsoleSelectMsg>(OnKeySelect);
            subr.Event<CharacterRecordsConsoleFilterMsg>(OnFilterApplied);
        });
    }

    private void OnFilterApplied(EntityUid entity, CharacterRecordConsoleComponent console,
        CharacterRecordsConsoleFilterMsg msg)
    {
        console.Filter = msg.Filter;
        UpdateUi(entity, console);
    }

    private void OnKeySelect(EntityUid entity, CharacterRecordConsoleComponent console,
        CharacterRecordConsoleSelectMsg msg)
    {
        console.Selected = msg.Key;
        UpdateUi(entity, console);
    }

    private void UpdateUi(EntityUid entity, CharacterRecordConsoleComponent? console = null)
    {
        if (!Resolve(entity, ref console))
            return;

        var station = _stationSystem.GetOwningStation(entity);
        if (!HasComp<StationRecordsComponent>(station) ||
            !HasComp<CharacterRecordsComponent>(station))
        {
            SendState(entity, new CharacterRecordConsoleState { ConsoleType = console.ConsoleType });
            return;
        }

        var characterRecords = _characterRecords.QueryRecords(station.Value);
        // Get the name and station records key display from the list of records
        var names = new Dictionary<NetEntity, (string, uint?)>();
        foreach (var (uid, r) in characterRecords)
        {
            // Apply any filter the user has set
            if (console.Filter != null)
            {
                if (IsSkippedRecord(console.Filter, r))
                    continue;
            }

            var netEnt = _entityManager.GetNetEntity(uid);
            if (names.ContainsKey(netEnt))
                Log.Error($"We somehow have duplicate character record keys, NetEntity: {netEnt}, Entity: {entity}, Character Name: {r.Name}");
            if (console.ConsoleType == RecordConsoleType.Admin)
            {
                // Admins get additional info to make it easier to run commands
                names[netEnt] = ($"{r.Name} ({netEnt}, {r.JobTitle}", r.StationRecordsKey);
            }
            else
            {
                names[netEnt] = ($"{r.Name} ({r.JobTitle})", r.StationRecordsKey);
            }
        }

        var record = console.Selected == null ? null : characterRecords[_entityManager.GetEntity(console.Selected.Value)];
        (SecurityStatus, string?)? securityStatus = null;

        // If we need the character's security status, gather it from the criminal records
        if ((console.ConsoleType == RecordConsoleType.Admin ||
            console.ConsoleType == RecordConsoleType.Security)
            && record?.StationRecordsKey != null)
        {
            var key = new StationRecordKey(record.StationRecordsKey.Value, station.Value);
            if (_stationRecords.TryGetRecord<CriminalRecord>(key, out var entry))
            {
                securityStatus = (entry.Status, entry.Reason);
            }
        }

        SendState(entity,
            new CharacterRecordConsoleState
            {
                ConsoleType = console.ConsoleType,
                RecordListing = names,
                Selected = console.Selected,
                SelectedRecord = record,
                Filter = console.Filter,
                SelectedSecurityStatus = securityStatus,
            });
    }

    private void SendState(EntityUid entity, CharacterRecordConsoleState state)
    {
        _userInterface.TrySetUiState(entity, CharacterRecordConsoleKey.Key, state);
    }

    /// <summary>
    /// Almost exactly the same as <see cref="StationRecordsSystem.IsSkipped"/>
    /// </summary>
    private static bool IsSkippedRecord(StationRecordsFilter filter,
        FullCharacterRecords record)
    {
        bool isFilter = filter.Value.Length > 0;

        if (!isFilter)
            return false;

        var filterLowerCaseValue = filter.Value.ToLower();

        return filter.Type switch
        {
            StationRecordFilterType.Name =>
                !record.Name.ToLower().Contains(filterLowerCaseValue),
            StationRecordFilterType.Prints => record.Fingerprint != null
                && IsFilterWithSomeCodeValue(record.Fingerprint, filterLowerCaseValue),
            StationRecordFilterType.DNA => record.DNA != null
                                                && IsFilterWithSomeCodeValue(record.DNA, filterLowerCaseValue),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid Character Record filter type"),
        };
    }

    private static bool IsFilterWithSomeCodeValue(string value, string filter)
    {
        return !value.ToLower().StartsWith(filter);
    }
}
