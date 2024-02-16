using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.StationRecords;
using Robust.Shared.Utility;

namespace Content.Server.StationRecords;

/// <summary>
///     Set of station records for a single station. StationRecordsComponent stores these.
///     Keyed by the record id, which should be obtained from
///     an entity that stores a reference to it.
///     A StationRecordKey has both the station entity (use to get the record set) and id (use for this).
/// </summary>
[DataDefinition]
public sealed partial class StationRecordSet
{
    [DataField("currentRecordId")]
    private uint _currentRecordId;

    /// <summary>
    /// Every key id that has a record(s) stored.
    /// Presumably this is faster than iterating the dictionary to check if any tables have a key.
    /// </summary>
    [DataField]
    public HashSet<uint> Keys = new();

    /// <summary>
    /// Recently accessed key ids which are used to synchronize them efficiently.
    /// </summary>
    [DataField]
    private HashSet<uint> _recentlyAccessed = new();

    /// <summary>
    /// Dictionary between a record's type and then each record indexed by id.
    /// </summary>
    [DataField]
    private Dictionary<Type, Dictionary<uint, object>> _tables = new();

    /// <summary>
    ///     Gets all records of a specific type stored in the record set.
    /// </summary>
    /// <typeparam name="T">The type of record to fetch.</typeparam>
    /// <returns>An enumerable object that contains a pair of both a station key, and the record associated with it.</returns>
    public IEnumerable<(uint, T)> GetRecordsOfType<T>()
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
    /// Create a new record with an entry.
    /// Returns an id that can only be used to access the record for this station.
    /// </summary>
    /// <param name="entry">Entry to add.</param>
    /// <typeparam name="T">Type of the entry that's being added.</typeparam>
    public uint? AddRecordEntry<T>(T entry)
    {
        if (entry == null)
            return null;

        var key = _currentRecordId++;
        AddRecordEntry(key, entry);
        return key;
    }

    /// <summary>
    ///     Add an entry into an existing record.
    /// </summary>
    /// <param name="key">Key id for the record.</param>
    /// <param name="entry">Entry to add.</param>
    /// <typeparam name="T">Type of the entry that's being added.</typeparam>
    public void AddRecordEntry<T>(uint key, T entry)
    {
        if (entry == null)
            return;

        Keys.Add(key);
        _tables.GetOrNew(typeof(T))[key] = entry;
    }

    /// <summary>
    ///     Try to get an record entry by type, from this record key.
    /// </summary>
    /// <param name="key">The record id to get the entries from.</param>
    /// <param name="entry">The entry that is retrieved from the record set.</param>
    /// <typeparam name="T">The type of entry to search for.</typeparam>
    /// <returns>True if the record exists and was retrieved, false otherwise.</returns>
    public bool TryGetRecordEntry<T>(uint key, [NotNullWhen(true)] out T? entry)
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
    /// <param name="key">The record key id.</param>
    /// <typeparam name="T">Type to check.</typeparam>
    /// <returns>True if the entry exists, false otherwise.</returns>
    public bool HasRecordEntry<T>(uint key)
    {
        return Keys.Contains(key)
               && _tables.TryGetValue(typeof(T), out var table)
               && table.ContainsKey(key);
    }

    /// <summary>
    ///     Get the recently accessed keys from this record set.
    /// </summary>
    /// <returns>All recently accessed keys from this record set.</returns>
    public IEnumerable<uint> GetRecentlyAccessed()
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
    /// Removes a recently accessed key from the set.
    /// </summary>
    public void RemoveFromRecentlyAccessed(uint key)
    {
        _recentlyAccessed.Remove(key);
    }

    /// <summary>
    ///     Removes all record entries related to this key from this set.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool RemoveAllRecords(uint key)
    {
        if (!Keys.Remove(key))
            return false;

        foreach (var table in _tables.Values)
        {
            table.Remove(key);
        }

        return true;
    }
}


