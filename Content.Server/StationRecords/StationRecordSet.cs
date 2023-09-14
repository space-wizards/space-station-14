using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.StationRecords;
using Robust.Shared.Utility;

namespace Content.Server.StationRecords;

/// <summary>
///     Set of station records. StationRecordsComponent stores these.
///     Keyed by StationRecordKey, which should be obtained from
///     an entity that stores a reference to it.
/// </summary>
[DataDefinition]
public sealed partial class StationRecordSet
{
    [DataField("currentRecordId")]
    private uint _currentRecordId;

    // TODO add custom type serializer so that keys don't have to be written twice.
    [DataField("keys")]
    public HashSet<StationRecordKey> Keys = new();

    [DataField("recentlyAccessed")]
    private HashSet<StationRecordKey> _recentlyAccessed = new();

    [DataField("tables")] // TODO ensure all of this data is serializable.
    private Dictionary<Type, Dictionary<StationRecordKey, object>> _tables = new();

    /// <summary>
    ///     Gets all records of a specific type stored in the record set.
    /// </summary>
    /// <typeparam name="T">The type of record to fetch.</typeparam>
    /// <returns>An enumerable object that contains a pair of both a station key, and the record associated with it.</returns>
    public IEnumerable<(StationRecordKey, T)> GetRecordsOfType<T>()
    {
        if (!_tables.ContainsKey(typeof(T)))
        {
            yield break;
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

    /// <summary>
    ///     Add an entry into a record.
    /// </summary>
    /// <param name="entry">Entry to add.</param>
    /// <typeparam name="T">Type of the entry that's being added.</typeparam>
    public StationRecordKey AddRecordEntry<T>(EntityUid station, T entry)
    {
        if (entry == null)
            return StationRecordKey.Invalid;

        var key = new StationRecordKey(_currentRecordId++, station);
        AddRecordEntry(key, entry);
        return key;
    }

    /// <summary>
    ///     Add an entry into a record.
    /// </summary>
    /// <param name="key">Key for the record.</param>
    /// <param name="entry">Entry to add.</param>
    /// <typeparam name="T">Type of the entry that's being added.</typeparam>
    public void AddRecordEntry<T>(StationRecordKey key, T entry)
    {
        if (entry == null)
            return;

        if (Keys.Add(key))
            _tables.GetOrNew(typeof(T))[key] = entry;
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

        if (!Keys.Contains(key)
            || !_tables.TryGetValue(typeof(T), out var table)
            || !table.TryGetValue(key, out var entryObject))
        {
            return false;
        }

        entry = (T) entryObject;
        _recentlyAccessed.Add(key);

        return true;
    }

    /// <summary>
    ///     Checks if the record associated with this key has an entry of a certain type.
    /// </summary>
    /// <param name="key">The record key.</param>
    /// <typeparam name="T">Type to check.</typeparam>
    /// <returns>True if the entry exists, false otherwise.</returns>
    public bool HasRecordEntry<T>(StationRecordKey key)
    {
        return Keys.Contains(key)
               && _tables.TryGetValue(typeof(T), out var table)
               && table.ContainsKey(key);
    }

    /// <summary>
    ///     Get the recently accessed keys from this record set.
    /// </summary>
    /// <returns>All recently accessed keys from this record set.</returns>
    public IEnumerable<StationRecordKey> GetRecentlyAccessed()
    {
        return _recentlyAccessed.ToArray();
    }

    /// <summary>
    ///     Clears the recently accessed keys from the set.
    /// </summary>
    public void ClearRecentlyAccessed()
    {
        _recentlyAccessed.Clear();
    }

    /// <summary>
    ///     Removes all record entries related to this key from this set.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool RemoveAllRecords(StationRecordKey key)
    {
        if (!Keys.Remove(key))
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


