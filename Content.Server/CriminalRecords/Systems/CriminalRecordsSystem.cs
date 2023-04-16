using Content.Server.StationRecords.Systems;
using Content.Shared.Inventory;
using Content.Shared.StationRecords;
using Content.Shared.Security;
using Robust.Shared.Prototypes;

namespace Content.Server.CriminalnRecords.Systems;

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

    public void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {

        var record = new GeneralStationRecord()
        {
            Name = ev.Record.Name,
            Age = ev.Record.Age,
            JobTitle = ev.Record.JobTitle,
            JobIcon = ev.Record.JobIcon,
            JobPrototype = ev.Record.JobPrototype,
            Species = ev.Record.Species,
            Gender = ev.Record.Gender,
            DisplayPriority = ev.Record.DisplayPriority,
            Fingerprint = ev.Record.Fingerprint,
            Status = SecurityStatus.None
        };

        _stationRecordsSystem.AddRecordEntry(ev.Key, record);
    }
}
