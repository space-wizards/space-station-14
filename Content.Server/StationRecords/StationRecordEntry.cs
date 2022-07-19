using System.Diagnostics.CodeAnalysis;
using Content.Shared.StationRecords;

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
public sealed class StationRecordSet
{
    private uint _currentRecordId;

    private HashSet<StationRecordKey> _keys = new();

    private HashSet<StationRecordKey> _recentlyAccessed = new();

    [ViewVariables]
    private Dictionary<Type, Dictionary<StationRecordKey, object>> _tables = new();

    // Gets all records of a specific type stored in the record set.
    public IEnumerable<(StationRecordKey, T)?> GetRecordsOfType<T>()
    {
        if (!_tables.ContainsKey(typeof(T)))
        {
            yield return null;
        }

        foreach (var (key, entry) in _tables[typeof(T)])
        {
            if (entry is not T cast)
            {
                continue;
            }

            _recentlyAccessed.Add(key);

            yield return (key, cast);
        }
    }

    // Add a record into this set of record entries.
    public StationRecordKey AddRecord(EntityUid station)
    {
        var key = new StationRecordKey(_currentRecordId++, station);

        _keys.Add(key);

        return key;
    }

    public void AddRecordEntry<T>(StationRecordKey key, T entry)
    {
        if (!_keys.Contains(key) || entry == null)
        {
            return;
        }

        if (!_tables.TryGetValue(typeof(T), out var table))
        {
            table = new();
            _tables.Add(typeof(T), table);
        }

        table.Add(key, entry);
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

        if (!_keys.Contains(key)
            || !_tables.TryGetValue(typeof(T), out var table)
            || !table.TryGetValue(key, out var entryObject))
        {
            return false;
        }

        entry = (T) entryObject;
        _recentlyAccessed.Add(key);

        return true;
    }

    public bool HasRecordEntry<T>(StationRecordKey key)
    {
        return _keys.Contains(key)
               && _tables.TryGetValue(typeof(T), out var table)
               && table.ContainsKey(key);
    }

    public IEnumerable<StationRecordKey> GetRecentlyAccessed()
    {
        return _recentlyAccessed;
    }

    public void ClearRecentlyAccessed()
    {
        _recentlyAccessed.Clear();
    }

    public bool RemoveAllRecords(StationRecordKey key)
    {
        if (!_keys.Remove(key))
        {
            return false;
        }

        foreach (var table in _tables.Values)
        {
            table.Remove(key);
        }

        return true;
    }
}


