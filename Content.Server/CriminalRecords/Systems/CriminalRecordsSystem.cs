using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Server.Forensics;
using Content.Server.Security.Components;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Inventory;
using Content.Shared.Nuke;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared.Security;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Security;

namespace Content.Server.CriminalnRecords.Systems;

/// <summary>
///     Criminal recordsss puke
/// </summary>
public sealed class CriminalRecordsSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly StationRecordKeyStorageSystem _keyStorageSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
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
