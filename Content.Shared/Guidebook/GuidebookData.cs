using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Guidebook;

/// <summary>
/// Used by GuidebookDataSystem to hold data extracted from prototype values,
/// both for storage and for network transmission.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class GuidebookData
{
    /// <summary>
    /// Total number of data values stored.
    /// </summary>
    [DataField]
    public int Count { get; private set; }

    /// <summary>
    /// The data extracted by the system.
    /// </summary>
    /// <remarks>
    /// Structured as PrototypeName, ComponentName, FieldName, Value
    /// </remarks>
    [DataField]
    public Dictionary<string, Dictionary<string, Dictionary<string, object?>>> Data = [];

    /// <summary>
    /// Adds a new value using the given identifiers.
    /// </summary>
    public void AddData(string prototype, string component, string field, object? value)
    {
        Data.GetOrNew(prototype).GetOrNew(component).Add(field, value);
        Count++;
    }

    /// <summary>
    /// Attempts to retrieve a value using the given identifiers.
    /// </summary>
    /// <returns>true if the value was retrieved, otherwise false</returns>
    public bool TryGetValue(string prototype, string component, string field, out object? value)
    {
        if (Data.TryGetValue(prototype, out var p)
            && p.TryGetValue(component, out var c)
            && c.TryGetValue(field, out value))
        {
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Deletes all data.
    /// </summary>
    public void Clear()
    {
        Data.Clear();
        Count = 0;
    }
}
