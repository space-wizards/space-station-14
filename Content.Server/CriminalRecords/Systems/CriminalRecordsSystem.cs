using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;

namespace Content.Server.CriminalRecords.Systems;

/// <summary>
///     Criminal records
/// </summary>
public sealed class CriminalRecordsSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {

        var record = new GeneralCriminalRecord()
        {
            Status = SecurityStatus.None,
            Reason = string.Empty
        };

        _stationRecordsSystem.AddRecordEntry(ev.Key, record);
        _stationRecordsSystem.Synchronize(ev.Key.OriginStation);
    }

    public bool TryChangeStatus(EntityUid station, StationRecordKey key, SecurityStatus status,
        out SecurityStatus updatedStatus, string? reason)
    {
        updatedStatus = default;

        if (!TryComp<StationRecordsComponent>(station, out var stationRecordsComponent))
            return false;

        _stationRecordsSystem.TryGetRecord(station, key, out GeneralCriminalRecord? record, stationRecordsComponent);

        if (status == record!.Status)
            return false;

        record.Reason = (status == SecurityStatus.None ? string.Empty : reason)!;
        record.Status = status;

        updatedStatus = record.Status;

        _stationRecordsSystem.Synchronize(station);

        return true;
    }

    public bool TryArrest(EntityUid station, StationRecordKey key, out SecurityStatus updatedStatus, string? reason)
    {
        updatedStatus = default;

        if (!TryComp<StationRecordsComponent>(station, out var stationRecordsComponent))
            return false;

        _stationRecordsSystem.TryGetRecord(station, key, out GeneralCriminalRecord? record, stationRecordsComponent);

        record!.Status = record.Status == SecurityStatus.Detained ? SecurityStatus.None : SecurityStatus.Detained;
        record.Reason = (record.Status == SecurityStatus.None ? string.Empty : reason)!;

        updatedStatus = record.Status;

        _stationRecordsSystem.Synchronize(station);

        return true;
    }
}
