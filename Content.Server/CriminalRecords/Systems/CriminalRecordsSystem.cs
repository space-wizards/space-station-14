using System.Diagnostics.CodeAnalysis;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;

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
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        var record = new CriminalRecord()
        {
            Status = SecurityStatus.None,
            Reason = string.Empty
        };

        _stationRecords.AddRecordEntry(ev.Key, record);
        _stationRecords.Synchronize(ev.Key);
    }

    /// <summary>
    /// Tries to change the status of the record found by the StationRecordKey
    /// </summary>
    /// <returns>True if the status is changed, false if not</returns>
    public bool TryChangeStatus(StationRecordKey key, SecurityStatus status,
        [NotNullWhen(true)] out SecurityStatus? updatedStatus, string? reason)
    {
        updatedStatus = null;

        if (!_stationRecords.TryGetRecord(key, out CriminalRecord? record)
            || status == record.Status)
            return false;

        record.Reason = (status == SecurityStatus.None ? string.Empty : reason)!;
        record.Status = status;

        updatedStatus = record.Status;

        _stationRecords.Synchronize(key);

        return true;
    }

    /// <summary>
    /// Tries to change the status of the record to Detained or None found by the StationRecordKey
    /// </summary>
    /// <returns>True if the status is changed, false if not</returns>
    public bool TryArrest(StationRecordKey key, [NotNullWhen(true)] out SecurityStatus? updatedStatus, string? reason)
    {
        updatedStatus = null;

        return TryChangeStatus(key, SecurityStatus.Detained, out updatedStatus, reason)
               || TryChangeStatus(key, SecurityStatus.None, out updatedStatus, reason);
    }
}
