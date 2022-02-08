using Robust.Shared.Random;

namespace Content.Server.Dynamic;

/// <summary>
///     Represents a weighted collection of items.
/// </summary>
/// <remarks>
///     Used for dynamic events, as well as storyteller prototypes.
/// </remarks>
public sealed class WeightedRandomCollection<T>
{
    private IRobustRandom _robustRandom;

    public WeightedRandomCollection(IRobustRandom? random=null)
    {
        IoCManager.Resolve(ref random);
        _robustRandom = random;
    }

    public struct Entry
    {
        public float AccumulatedWeight;
        public T Item;
    }

    private List<Entry> _entries = new();
    private float _accumulatedWeight;

    public void AddEntry(T item, float weight)
    {
        _accumulatedWeight += weight;
        _entries.Add(new Entry { AccumulatedWeight = _accumulatedWeight, Item = item });
    }

    public T Pick()
    {
        float rand = _robustRandom.NextFloat() * _accumulatedWeight;

        foreach (var entry in _entries)
        {
            if (entry.AccumulatedWeight >= rand)
            {
                return entry.Item;
            }
        }

        // fallback
        return _robustRandom.Pick(_entries).Item;
    }
}
