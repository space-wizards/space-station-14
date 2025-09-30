using System;
using System.Collections.Generic;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using Robust.Server.GameObjects;

namespace Content.Server._CD.Records.Consoles;

/// <summary>
/// Drives the BUI for the Cosmatic Drift record consoles.
/// </summary>
public sealed class CharacterRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly CharacterRecordsSystem _characterRecords = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterRecordConsoleComponent, CharacterRecordsModifiedEvent>((uid, component, _) =>
            UpdateUi(uid, component));

        Subs.BuiEvents<CharacterRecordConsoleComponent>(CharacterRecordConsoleKey.Key,
            subscriber =>
            {
                subscriber.Event<BoundUIOpenedEvent>((uid, component, _) => UpdateUi(uid, component));
                subscriber.Event<CharacterRecordConsoleSelectMsg>(OnKeySelect);
                subscriber.Event<CharacterRecordsConsoleFilterMsg>(OnFilterApplied);
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

    private void UpdateUi(EntityUid entity, CharacterRecordConsoleComponent? console = null)
    {
        if (!Resolve(entity, ref console))
            return;

        var station = _station.GetOwningStation(entity);
        // When the console is not tied to a valid station datastore, fall back to an empty UI state.
        if (!HasComp<StationRecordsComponent>(station) ||
            !HasComp<CharacterRecordsComponent>(station))
        {
            SendState(entity, new CharacterRecordConsoleState { ConsoleType = console.ConsoleType });
            return;
        }

        var characterRecords = _characterRecords.QueryRecords(station.Value);
        var names = new Dictionary<uint, CharacterRecordConsoleState.CharacterInfo>();
        foreach (var (key, record) in characterRecords)
        {
            var netEntity = _entityManager.GetNetEntity(record.Owner!.Value);
            var displayName = console.ConsoleType != RecordConsoleType.Admin
                ? $"{record.Name} ({record.JobTitle})"
                : $"{record.Name} ({netEntity}, {record.JobTitle})";

            // Allow local filtering before the entry shows up in the UI list.
            if (console.Filter != null && IsSkippedRecord(console.Filter, record, displayName))
                continue;

            if (names.ContainsKey(key))
            {
                Log.Error($"Duplicate character record key {key} for console {ToPrettyString(entity)}");
                continue;
            }

            names[key] = new CharacterRecordConsoleState.CharacterInfo
            {
                CharacterDisplayName = displayName,
                StationRecordKey = record.StationRecordsKey,
            };
        }

        var selectedRecord = console.SelectedIndex != null
            && characterRecords.TryGetValue(console.SelectedIndex.Value, out var value)
                ? value
                : null;

        (SecurityStatus, string?)? securityStatus = null;
        if ((console.ConsoleType == RecordConsoleType.Admin || console.ConsoleType == RecordConsoleType.Security)
            && selectedRecord?.StationRecordsKey != null)
        {
            // Security-facing consoles surface the linked criminal record for quick context.
            var key = new StationRecordKey(selectedRecord.StationRecordsKey.Value, station.Value);
            if (_records.TryGetRecord<CriminalRecord>(key, out var entry))
                securityStatus = (entry.Status, entry.Reason);
        }

        SendState(entity,
            new CharacterRecordConsoleState
            {
                ConsoleType = console.ConsoleType,
                CharacterList = names,
                SelectedIndex = console.SelectedIndex,
                SelectedRecord = selectedRecord,
                Filter = console.Filter,
                SelectedSecurityStatus = securityStatus,
            });
    }

    private void SendState(EntityUid entity, CharacterRecordConsoleState state)
    {
        _ui.SetUiState(entity, CharacterRecordConsoleKey.Key, state);
    }

    private static bool IsSkippedRecord(StationRecordsFilter filter, FullCharacterRecords record, string nameJob)
    {
        // Each console type exposes a slightly different search surface; bail out early when nothing was typed.
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
        // DNA / fingerprint filters only care about the prefix, mirroring the console UI expectations.
        return !value.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase);
    }
}
