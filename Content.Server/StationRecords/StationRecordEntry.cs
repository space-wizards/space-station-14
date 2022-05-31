using System.Diagnostics.CodeAnalysis;

namespace Content.Server.StationRecords;

/// <summary>
///     Set of station records. StationRecordsComponent stores these.
///     Keyed by StationRecordKey, which should be obtained from
///     an entity that stores a reference to it.
///
///     StationRecordsSystem should verify that all added records are
///     correct, and all keys originate from the station that owns
///     this component.
/// </summary>
public sealed class StationRecordSet : Dictionary<StationRecordKey, StationRecordEntry>
{
    private uint _currentRecordId;

    // Gets all records of a specific type stored in the record set.
    public IEnumerable<(StationRecordKey, T)> GetRecordsOfType<T>()
    {
        foreach (var (key, entry) in this)
        {
            if (entry.Entries.TryGetValue(typeof(T), out var obj)
                && obj is T cast)
            {
                yield return (key, cast);
            }
        }
    }

    // Gets all records of a specific type stored in the record set.
    public IEnumerable<(StationRecordKey, T1, T2)> GetRecordsOfType<T1, T2>()
    {
        foreach (var (key, entry) in this)
        {
            if (entry.Entries.TryGetValue(typeof(T1), out var objT)
                && entry.Entries.TryGetValue(typeof(T2), out var objU)
                && objT is T1 castT
                && objU is T2 castU)
            {
                yield return (key, castT, castU);
            }
        }
    }

    // Add a record into this set of record entries.
    public (StationRecordKey, StationRecordEntry) AddRecord(EntityUid station)
    {
        var key = new StationRecordKey(_currentRecordId++, station);
        var record = new StationRecordEntry();

        Add(key, record);

        return (key, record);
    }

    public void AddRecordEntry<T>(StationRecordKey key, T entry)
    {
        if (!TryGetValue(key, out var record))
        {
            return;
        }

        if (record.Entries.ContainsKey(typeof(T)))
        {
            record.Entries[typeof(T)] = entry!;
        }
        else
        {
            record.Entries.Add(typeof(T), entry!);
        }
    }

    /// <summary>
    ///     Try to get an record entry by type, from this record key.
    /// </summary>
    /// <param name="key">The StationRecordKey to get the entries from.</param>
    /// <param name="entry">The entry that is retrieved from the record set.</param>
    /// <typeparam name="T">The type of entry to search for.</typeparam>
    /// <returns>True if the record exists and was retrieved, false otherwise.</returns>
    public bool TryGetRecordEntry<T>(StationRecordKey key, [NotNullWhen(true)] out T? entry)
    {
        entry = default;

        if (!TryGetValue(key, out var record))
        {
            return false;
        }

        if (record.Entries.TryGetValue(typeof(T), out var entryObj)
            && entryObj is T cast)
        {
            entry = cast;
            return true;
        }

        return false;
    }
}

public sealed class StationRecordEntry
{
    // Record entries. This contains information about a crewmember
    // without tying it to the entity UID of that crewmember entity.
    public Dictionary<Type, object> Entries = new();
}

// Station record keys. These should be stored somewhere,
// preferably within an ID card.
public readonly struct StationRecordKey
{
    public uint ID { get; }
    public EntityUid OriginStation { get; }

    public StationRecordKey(uint id, EntityUid originStation)
    {
        ID = id;
        OriginStation = originStation;
    }
}

// Station record types. This could be by string,
// but it is instead an enum to encourage more
// cleaner prototypes. Records should be implemented
// within code anyways.
public enum StationRecordType : byte
{
    General
}
