// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StationRecords;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CriminalRecords;
public sealed class CriminalRecordSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IAdminLogManager _logManager = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("CriminalRecords");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, ExaminedEvent>(OnStatusExamine);
    }

    // TheArturZh 25.09.2023 22:15
    // TODO: bad code. make it use InventoryRelayedEvent. Create separate components for examining and for examined subscription.
    // no pohuy prosto zaebalsya(
    private void OnStatusExamine(EntityUid uid, StatusIconComponent comp, ExaminedEvent args)
    {
        var scannerOn = false;
        if (_inventory.TryGetSlotEntity(args.Examiner, "eyes", out var ent))
        {
            if (HasComp<ShowSecurityIconsComponent>(ent))
            {
                scannerOn = true;
            }
        }

        if (!scannerOn)
            return;

        CriminalRecord? record = null;

        if (_accessReader.FindAccessItemsInventory(uid, out var items))
        {
            foreach (var item in items)
            {
                // ID Card
                if (TryComp(item, out IdCardComponent? id))
                {
                    if (id.CurrentSecurityRecord != null)
                    {
                        record = id.CurrentSecurityRecord;
                        break;
                    }
                }

                // PDA
                if (TryComp(item, out PdaComponent? pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    if (id.CurrentSecurityRecord != null)
                    {
                        record = id.CurrentSecurityRecord;
                        break;
                    }
                }
            }
        }

        //SS220 Criminal-Records begin
        if (record != null)
        {
            var msg = new FormattedMessage();

            if (record.RecordType == null)
            {
                msg.AddMarkup("[bold]Без статуса: [/bold]");
            }
            else
            {
                if (_prototype.TryIndex<CriminalStatusPrototype>(record.RecordType, out var statusType))
                {
                    msg.AddMarkup($"[color={statusType.Color.ToHex()}][bold]{statusType.Name}:[/bold][/color] ");
                }
            }

            msg.AddText(record.Message);
            args.PushMessage(msg);
        }
    }

    public CriminalRecordCatalog EnsureRecordCatalog(GeneralStationRecord record)
    {
        if (record.CriminalRecords != null)
            return record.CriminalRecords;

        var catalog = new CriminalRecordCatalog();
        record.CriminalRecords = catalog;
        return catalog;
    }

    // for record removal
    public void UpdateLastRecordTime(CriminalRecordCatalog catalog)
    {
        var biggest = -1;
        foreach (var time in catalog.Records.Keys)
        {
            if (time > biggest)
                biggest = time;
        }

        catalog.LastRecordTime = biggest == -1 ? null : biggest;
    }

    public void UpdateIdCards((NetEntity, uint) key, GeneralStationRecord generalRecord)
    {
        CriminalRecord? criminalRecord = null;
        if (generalRecord.CriminalRecords != null)
        {
            if (generalRecord.CriminalRecords.LastRecordTime is int lastRecordTime)
            {
                generalRecord.CriminalRecords.Records.TryGetValue(lastRecordTime, out criminalRecord);
            }
        }

        var stationUid = GetEntity(key.Item1);
        var query = EntityQueryEnumerator<IdCardComponent, StationRecordKeyStorageComponent>();

        while (query.MoveNext(out var uid, out var idCard, out var keyStorage))
        {
            if (!keyStorage.Key.HasValue)
                continue;

            if (keyStorage.Key.Value.Id != key.Item2 || keyStorage.Key.Value.OriginStation != stationUid)
            {
                continue;
            }

            idCard.CurrentSecurityRecord = criminalRecord;
            EntityManager.Dirty(uid, idCard);
        }
    }

    public bool RemoveCriminalRecordStatus((NetEntity, uint) key, int time, ICommonSession? sender = null)
    {
        if (!TryGetEntity(key.Item1, out var station))
            return false;

        if (!_stationRecords.TryGetRecord(
            station.Value,
            _stationRecords.Convert(key),
            out GeneralStationRecord? selectedRecord))
        {
            _sawmill.Warning("Tried to add a criminal record but can't get a general record.");
            return false;
        }

        // If it is the same status with the same message - drop it to prevent spam
        var catalog = EnsureRecordCatalog(selectedRecord);
        if (!catalog.Records.Remove(time))
            return false;

        UpdateLastRecordTime(catalog);
        _stationRecords.Synchronize(station.Value);
        UpdateIdCards(key, selectedRecord);

        if (sender != null)
        {
            _logManager.Add(
                LogType.SecutiyRecords,
                LogImpact.High,
                $"{ToPrettyString(sender.AttachedEntity):user} DELETED a criminal record for {selectedRecord.Name} with ID {time}"
            );
        }

        return true;
    }

    public bool TryGetLastRecord(
        (NetEntity, uint) key,
        [NotNullWhen(true)] out GeneralStationRecord? stationRecord,
        [NotNullWhen(true)] out CriminalRecord? criminalRecord)
    {
        stationRecord = null;
        criminalRecord = null;

        if (!TryGetEntity(key.Item1, out var station))
            return false;

        if (!_stationRecords.TryGetRecord(
            station.Value,
            _stationRecords.Convert(key),
            out stationRecord))
        {
            return false;
        }

        if (stationRecord.CriminalRecords is not CriminalRecordCatalog catalog)
            return false;

        criminalRecord = catalog.GetLastRecord();
        return criminalRecord != null;
    }

    public bool AddCriminalRecordStatus((NetEntity, uint) key, string message, string? statusPrototypeId, ICommonSession? sender = null)
    {
        if (!TryGetEntity(key.Item1, out var station))
            return false;

        if (!_stationRecords.TryGetRecord(
            station.Value,
            _stationRecords.Convert(key),
            out GeneralStationRecord? selectedRecord))
        {
            _sawmill.Warning("Tried to add a criminal record but can't get a general record.");
            return false;
        }

        var catalog = EnsureRecordCatalog(selectedRecord);

        ProtoId<CriminalStatusPrototype>? validatedRecordType = null;
        if (statusPrototypeId != null)
        {
            if (_prototype.HasIndex<CriminalStatusPrototype>(statusPrototypeId))
                validatedRecordType = statusPrototypeId;
        }

        // If it is the same status with the same message - drop it to prevent spam
        if (catalog.LastRecordTime.HasValue)
        {
            if (catalog.Records.TryGetValue(catalog.LastRecordTime.Value, out var lastRecord))
            {
                if (lastRecord.RecordType?.Id == statusPrototypeId && message == lastRecord.Message)
                    return false;
            }
        }

        var criminalRecord = new CriminalRecord()
        {
            Message = message,
            RecordType = validatedRecordType
        };

        var currentRoundTime = (int) _gameTicker.RoundDuration().TotalSeconds;
        if (!catalog.Records.TryAdd(currentRoundTime, criminalRecord))
            return false;

        catalog.LastRecordTime = currentRoundTime;
        _stationRecords.Synchronize(station.Value);
        UpdateIdCards(key, selectedRecord);
        _sawmill.Debug("Added new criminal record, synchonizing");

        if (sender != null)
        {
            _logManager.Add(
                LogType.SecutiyRecords,
                statusPrototypeId == "execute" ? LogImpact.Extreme : LogImpact.High,
                $"{ToPrettyString(sender.AttachedEntity):user} sent a new criminal record for {selectedRecord.Name} with ID {currentRoundTime} with type '{statusPrototypeId ?? "none"}' with message: {message}"
            );
        }

        return true;
    }
}
