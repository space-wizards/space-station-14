using System.Diagnostics.CodeAnalysis;
using Content.Shared.Random.Helpers;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Events;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared.StationRecords.Systems;

public sealed partial class StationRecordsSystem
{
    /// <summary>
    /// Set the station records key for an id/pda.
    /// </summary>
    [PublicAPI]
    public void SetIdKey(EntityUid? uid, StationRecordKey key)
    {
        if (uid is not {} idUid)
            return;

        var keyStorageEntity = idUid;
        if (_pdaQuery.TryComp(idUid, out var pda) && pda.ContainedId is {} id)
        {
            keyStorageEntity = id;
        }

        _keyStorage.AssignKey(keyStorageEntity, key);
    }

    /// <summary>
    ///     Removes a record from this station.
    /// </summary>
    /// <param name="key">The station and key to remove.</param>
    /// <param name="records">Station records component.</param>
    /// <returns>True if the record was removed, false otherwise.</returns>
    [PublicAPI]
    public bool RemoveRecord(StationRecordKey key, StationRecordsComponent? records = null)
    {
        if (!_recordsQuery.Resolve(key.OriginStation, ref records))
            return false;

        if (!records.Records.RemoveAllRecords(key.Id))
            return false;

        var ev = new RecordRemovedEvent(key);
        RaiseLocalEvent(ref ev);

        Dirty(key.OriginStation, records);
        return true;
    }

    /// <summary>
    /// Gets a random record from the station's record entries.
    /// </summary>
    /// <param name="ent">The EntityId of the station from which you want to get the record.</param>
    /// <param name="entry">The resulting entry.</param>
    /// <param name="seedEntity">An optional entity to use as a seed (see remarks).</param>
    /// <typeparam name="T">Type to get from the record set.</typeparam>
    /// <returns>True if a record was obtained. False otherwise.</returns>
    /// <remarks><see cref="seedEntity"/> should be used where possible with predicted randomness to prevent repeated values within a tick.</remarks>
    [PublicAPI]
    public bool TryGetRandomRecord<T>(Entity<StationRecordsComponent?> ent, [NotNullWhen(true)] out T? entry, EntityUid? seedEntity = null) where T : StationRecord
    {
        entry = default;

        if (!_recordsQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (ent.Comp.Records.Keys.Count == 0)
            return false;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(seedEntity ?? ent.Owner));
        var key = random.Pick(ent.Comp.Records.Keys);

        return ent.Comp.Records.TryGetRecordEntry(key, out entry);
    }

    /// <summary>
    /// Get the name for a record, or an empty string if it has no record.
    /// </summary>
    [PublicAPI]
    public string RecordName(StationRecordKey key)
    {
        return !TryGetRecord<GeneralStationRecord>(key, out var record) ? string.Empty : record.Name;
    }

    /// <summary>
    ///     Adds a new record entry to a station's record set.
    /// </summary>
    /// <param name="station">The station to add the record to.</param>
    /// <param name="record">The record to add.</param>
    /// <typeparam name="T">The type of record to add.</typeparam>
    [PublicAPI]
    public StationRecordKey AddRecordEntry<T>(Entity<StationRecordsComponent?> station, T record) where T : StationRecord
    {
        if (!_recordsQuery.Resolve(station, ref station.Comp))
            return StationRecordKey.Invalid;

        var id = station.Comp.Records.AddRecordEntry(record);
        Dirty(station);
        return id == null ? StationRecordKey.Invalid : new StationRecordKey(id.Value, station);
    }

    /// <summary>
    /// Adds a record to an existing entry.
    /// </summary>
    /// <param name="key">The station and id of the existing entry.</param>
    /// <param name="record">The record to add.</param>
    /// <param name="records">Station records component.</param>
    /// <typeparam name="T">The type of record to add.</typeparam>
    [PublicAPI]
    public void AddRecordEntry<T>(StationRecordKey key,
        T record,
        StationRecordsComponent? records = null) where T : StationRecord
    {
        if (!_recordsQuery.Resolve(key.OriginStation, ref records))
            return;

        records.Records.AddRecordEntry(key.Id, record);
        Dirty(key.OriginStation, records);
    }

    /// <summary>
    ///     Synchronizes a station's records with any systems that need it.
    /// </summary>
    /// <param name="station">The station to synchronize any recently accessed records with.</param>
    [PublicAPI]
    public void Synchronize(Entity<StationRecordsComponent?> station)
    {
        if (!_recordsQuery.Resolve(station, ref station.Comp))
            return;

        foreach (var key in station.Comp.Records.GetRecentlyAccessed())
        {
            var ev = new RecordModifiedEvent(new StationRecordKey(key, station));
            RaiseLocalEvent(ref ev);
        }

        station.Comp.Records.ClearRecentlyAccessed();
        Dirty(station);
    }

    /// <summary>
    /// Synchronizes a single record's entries for a station.
    /// </summary>
    /// <param name="key">The station and id of the record</param>
    /// <param name="records">Station records component.</param>
    [PublicAPI]
    public void Synchronize(StationRecordKey key, StationRecordsComponent? records = null)
    {
        if (!_recordsQuery.Resolve(key.OriginStation, ref records))
            return;

        var ev = new RecordModifiedEvent(key);
        RaiseLocalEvent(ref ev);

        records.Records.RemoveFromRecentlyAccessed(key.Id);
        Dirty(key.OriginStation, records);
    }

    /// <summary>
    ///     Try to get a record from this station's record entries,
    ///     from the provided station record key. Will always return
    ///     null if the key does not match the station.
    /// </summary>
    /// <param name="key">Station and key to try and index from the record set.</param>
    /// <param name="entry">The resulting entry.</param>
    /// <param name="records">Station record component.</param>
    /// <typeparam name="T">Type to get from the record set.</typeparam>
    /// <returns>True if the record was obtained, false otherwise. Always false on client.</returns>
    [PublicAPI]
    public bool TryGetRecord<T>(StationRecordKey key, [NotNullWhen(true)] out T? entry, StationRecordsComponent? records = null) where T : StationRecord
    {
        entry = null;
        if (!_recordsQuery.Resolve(key.OriginStation, ref records))
            return false;

        return records.Records.TryGetRecordEntry(key.Id, out entry);
    }

    /// <summary>
    ///     Gets all records of a specific type from a station.
    /// </summary>
    /// <param name="station">The station to get the records from.</param>
    /// <typeparam name="T">Type of record to fetch</typeparam>
    /// <returns>Enumerable of pairs with a station record key, and the entry in question of type T. Always empty on client.</returns>
    [PublicAPI]
    public IEnumerable<(uint, T)> GetRecordsOfType<T>(Entity<StationRecordsComponent?> station)
    {
        if (!_recordsQuery.Resolve(station, ref station.Comp))
            return Array.Empty<(uint, T)>();

        return station.Comp.Records.GetRecordsOfType<T>();
    }

    /// <summary>
    /// Returns an id if a record with the same name exists.
    /// </summary>
    /// <remarks>
    /// Linear search so O(n) time complexity.
    /// </remarks>
    /// <returns>Returns a station record id. Always null on client.</returns>
    [PublicAPI]
    public uint? GetRecordByName(Entity<StationRecordsComponent?> station, string name)
    {
        if (!_recordsQuery.Resolve(station, ref station.Comp, false))
            return null;

        foreach (var (id, record) in GetRecordsOfType<GeneralStationRecord>(station))
        {
            if (record.Name == name)
                return id;
        }

        return null;
    }
}
