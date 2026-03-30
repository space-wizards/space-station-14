using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.StationRecords;

public abstract class SharedStationRecordsSystem : EntitySystem
{
    public StationRecordKey? Convert((NetEntity, uint)? input)
    {
        return input == null ? null : Convert(input.Value);
    }

    public (NetEntity, uint)? Convert(StationRecordKey? input)
    {
        return input == null ? null : Convert(input.Value);
    }

    public StationRecordKey Convert((NetEntity, uint) input)
    {
        return new StationRecordKey(input.Item2, GetEntity(input.Item1));
    }
    public (NetEntity, uint) Convert(StationRecordKey input)
    {
        return (GetNetEntity(input.OriginStation), input.Id);
    }

    public List<(NetEntity, uint)> Convert(ICollection<StationRecordKey> input)
    {
        var result = new List<(NetEntity, uint)>(input.Count);
        foreach (var entry in input)
        {
            result.Add(Convert(entry));
        }
        return result;
    }

    public List<StationRecordKey> Convert(ICollection<(NetEntity, uint)> input)
    {
        var result = new List<StationRecordKey>(input.Count);
        foreach (var entry in input)
        {
            result.Add(Convert(entry));
        }
        return result;
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
    public bool TryGetRecord<T>(StationRecordKey key, [NotNullWhen(true)] out T? entry, StationRecordsComponent? records = null)
    {
        entry = default;

        if (!Resolve(key.OriginStation, ref records))
            return false;

        return records.Records.TryGetRecordEntry(key.Id, out entry);
    }

    /// <summary>
    ///     Gets all records of a specific type from a station.
    /// </summary>
    /// <param name="station">The station to get the records from.</param>
    /// <param name="records">Station records component.</param>
    /// <typeparam name="T">Type of record to fetch</typeparam>
    /// <returns>Enumerable of pairs with a station record key, and the entry in question of type T. Always empty on client.</returns>
    public IEnumerable<(uint, T)> GetRecordsOfType<T>(EntityUid station, StationRecordsComponent? records = null)
    {
        if (!Resolve(station, ref records))
            return Array.Empty<(uint, T)>();

        return records.Records.GetRecordsOfType<T>();
    }

    /// <summary>
    /// Returns an id if a record with the same name exists.
    /// </summary>
    /// <remarks>
    /// Linear search so O(n) time complexity.
    /// </remarks>
    /// <returns>Returns a station record id. Always null on client.</returns>
    public uint? GetRecordByName(EntityUid station, string name, StationRecordsComponent? records = null)
    {
        if (!Resolve(station, ref records, false))
            return null;

        foreach (var (id, record) in GetRecordsOfType<GeneralStationRecord>(station, records))
        {
            if (record.Name == name)
                return id;
        }

        return null;
    }
}
