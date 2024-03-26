using System.Diagnostics.CodeAnalysis;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Timing;

namespace Content.Server.CriminalRecords.Systems;

/// <summary>
///     Criminal records
///
///     Criminal Records inherit Station Records' core and add role-playing tools for Security:
///         - Ability to track a person's status (Detained/Wanted/None)
///         - See security officers' actions in Criminal Records in the radio
///         - See reasons for any action with no need to ask the officer personally
/// </summary>
public sealed class CriminalRecordsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        _stationRecords.AddRecordEntry(ev.Key, new CriminalRecord());
        _stationRecords.Synchronize(ev.Key);
    }

    /// <summary>
    /// Tries to change the status of the record found by the StationRecordKey.
    /// Reason should only be passed if status is Wanted.
    /// </summary>
    /// <returns>True if the status is changed, false if not</returns>
    public bool TryChangeStatus(StationRecordKey key, SecurityStatus status, string? reason)
    {
        // don't do anything if its the same status
        if (!_stationRecords.TryGetRecord<CriminalRecord>(key, out var record)
            || status == record.Status)
            return false;

        record.Status = status;
        record.Reason = reason;

        _stationRecords.Synchronize(key);

        return true;
    }

    /// <summary>
    /// Tries to add a history entry to a criminal record.
    /// </summary>
    /// <returns>True if adding succeeded, false if not</returns>
    public bool TryAddHistory(StationRecordKey key, CrimeHistory entry)
    {
        if (!_stationRecords.TryGetRecord<CriminalRecord>(key, out var record))
            return false;

        record.History.Add(entry);
        return true;
    }

    /// <summary>
    /// Creates and tries to add a history entry using the current time.
    /// </summary>
    public bool TryAddHistory(StationRecordKey key, string line)
    {
        var entry = new CrimeHistory(_timing.CurTime, line);
        return TryAddHistory(key, entry);
    }

    /// <summary>
    /// Tries to delete a sepcific line of history from a criminal record, by index.
    /// </summary>
    /// <returns>True if the line was removed, false if not</returns>
    public bool TryDeleteHistory(StationRecordKey key, uint index)
    {
        if (!_stationRecords.TryGetRecord<CriminalRecord>(key, out var record))
            return false;

        if (index >= record.History.Count)
            return false;

        record.History.RemoveAt((int) index);
        return true;
    }
}
