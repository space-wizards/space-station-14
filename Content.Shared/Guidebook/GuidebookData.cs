using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Guidebook;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class GuidebookData
{
    public int Count { get; private set; }
    public Dictionary<string, Dictionary<string, Dictionary<string, object?>>> Data = [];

    public void AddData(string prototype, string component, string field, object? value)
    {
        Data.GetOrNew(prototype).GetOrNew(component).Add(field, value);
        Count++;
    }

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

    public void Clear()
    {
        Data.Clear();
        Count = 0;
    }
}
