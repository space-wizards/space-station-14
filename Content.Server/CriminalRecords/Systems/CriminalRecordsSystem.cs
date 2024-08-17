using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.CartridgeLoader;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CriminalRecords.Systems;

/// <summary>
///     Criminal records
///
///     Criminal Records inherit Station Records' core and add role-playing tools for Security:
///         - Ability to track a person's status (Detained/Wanted/None)
///         - See security officers' actions in Criminal Records in the radio
///         - See reasons for any action with no need to ask the officer personally
/// </summary>
public sealed class CriminalRecordsSystem : SharedCriminalRecordsSystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
        SubscribeLocalEvent<WantedListCartridgeComponent, CriminalRecordChangedEvent>(OnRecordChanged);
        SubscribeLocalEvent<WantedListCartridgeComponent, CartridgeUiReadyEvent>(OnCartridgeUiReady);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        _records.AddRecordEntry(ev.Key, new CriminalRecord());
        _records.Synchronize(ev.Key);
    }

    /// <summary>
    /// Tries to change the status of the record found by the StationRecordKey.
    /// Reason should only be passed if status is Wanted, nullability isn't checked.
    /// </summary>
    /// <returns>True if the status is changed, false if not</returns>
    public bool TryChangeStatus(StationRecordKey key, SecurityStatus status, string? reason)
    {
        // don't do anything if its the same status
        if (!_records.TryGetRecord<CriminalRecord>(key, out var record)
            || status == record.Status)
            return false;

        OverwriteStatus(key, record, status, reason);

        return true;
    }

    /// <summary>
    /// Sets the status without checking previous status or reason nullability.
    /// </summary>
    public void OverwriteStatus(StationRecordKey key, CriminalRecord record, SecurityStatus status, string? reason)
    {
        record.Status = status;
        record.Reason = reason;

        var name = _records.RecordName(key);
        if (name != string.Empty)
            UpdateCriminalIdentity(name, status);

        _records.Synchronize(key);

        var args = new CriminalRecordChangedEvent(record);
        var query = EntityQueryEnumerator<WantedListCartridgeComponent>();
        while (query.MoveNext(out var readerUid, out _))
        {
            RaiseLocalEvent(readerUid, ref args);
        }
    }

    /// <summary>
    /// Tries to add a history entry to a criminal record.
    /// </summary>
    /// <returns>True if adding succeeded, false if not</returns>
    public bool TryAddHistory(StationRecordKey key, CrimeHistory entry)
    {
        if (!_records.TryGetRecord<CriminalRecord>(key, out var record))
            return false;

        record.History.Add(entry);
        return true;
    }

    /// <summary>
    /// Creates and tries to add a history entry using the current time.
    /// </summary>
    public bool TryAddHistory(StationRecordKey key, string line)
    {
        var entry = new CrimeHistory(_ticker.RoundDuration(), line);
        return TryAddHistory(key, entry);
    }

    /// <summary>
    /// Tries to delete a sepcific line of history from a criminal record, by index.
    /// </summary>
    /// <returns>True if the line was removed, false if not</returns>
    public bool TryDeleteHistory(StationRecordKey key, uint index)
    {
        if (!_records.TryGetRecord<CriminalRecord>(key, out var record))
            return false;

        if (index >= record.History.Count)
            return false;

        record.History.RemoveAt((int) index);
        return true;
    }

    /// <summary>
    /// Returns a collection of all criminal records.
    /// </summary>
    /// <param name="station">The station to get the records from.</param>
    /// <param name="filter">A filter to retrieve a specific group of records.</param>
    public IEnumerable<WantedRecord> GetRecords(EntityUid station, CriminalRecordsFilter filter = CriminalRecordsFilter.None)
    {
        // Get the target name and convert the collection from (uint, CriminalRecord) to WantedRecord.
        var records = _records.GetRecordsOfType<CriminalRecord>(station).Select(cr =>
        {
            var (key, r) = cr;
            var target = _records.RecordName(new (key, station));
            return new WantedRecord(target, r.Status, r.Reason);
        });

        if (filter == CriminalRecordsFilter.None)
            return records;

        return records.Where(cr => filter switch
        {
            CriminalRecordsFilter.Wanted => cr.Status is SecurityStatus.Wanted or SecurityStatus.Suspected,
            CriminalRecordsFilter.Released => cr.Status is SecurityStatus.Detained or SecurityStatus.Discharged or SecurityStatus.Paroled,
            _ => cr.Status == SecurityStatus.None,
        });
    }

    private void OnRecordChanged(Entity<WantedListCartridgeComponent> ent, ref CriminalRecordChangedEvent args)
    {
        if (Comp<CartridgeComponent>(ent).LoaderUid is not { } loaderUid)
            return;

        UpdateReaderUi(ent, loaderUid);
    }

    private void OnCartridgeUiReady(Entity<WantedListCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateReaderUi(ent, args.Loader);
    }

    private void UpdateReaderUi(Entity<WantedListCartridgeComponent> ent, EntityUid loaderUid)
    {
        if (_station.GetOwningStation(ent) is not { } station)
            return;

        var records = GetRecords(station, CriminalRecordsFilter.Wanted);
        var state = new WantedListUiState(records.ToList());

        _cartridge.UpdateCartridgeUiState(loaderUid, state);
    }
}

public enum CriminalRecordsFilter
{
    /// <summary>
    /// All records.
    /// </summary>
    None,

    /// <summary>
    /// All records with <c>Wanted</c> and <c>Suspected</c> statuses.
    /// </summary>
    Wanted,

    /// <summary>
    /// All records with the status <c>Detained</c>, <c>Discharged</c> and <c>Paroled</c>.
    /// </summary>
    Released,
}
