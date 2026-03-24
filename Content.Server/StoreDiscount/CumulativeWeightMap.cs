using System.Linq;
using Content.Shared.Store;
using Robust.Shared.Random;

namespace Content.Server.StoreDiscount;

/// <summary>
/// Weighted category selection map using a cumulative-weight array for O(n) sampling.
/// Used by <see cref="Systems.StoreDiscountSystem"/> and <see cref="Systems.SecondHandSystem"/>
/// to pick a random set of categories respecting per-category weights and item caps.
/// </summary>
/// <typeparam name="T">A category prototype implementing <see cref="IWeightedCategory"/>.</typeparam>
public sealed class CumulativeWeightMap<T> where T : class, IWeightedCategory
{
    private readonly List<T> _categories;
    private readonly List<int> _weights; // cumulative; index i holds sum of weights[0..i]
    private int _totalWeight;

    /// <summary>
    /// Builds the map from a set of category prototypes.
    /// Categories with zero or negative weight, or a zero/negative MaxItems cap, are excluded.
    /// Categories with null MaxItems are included and will never be auto-removed.
    /// </summary>
    public CumulativeWeightMap(IEnumerable<T> prototypes)
    {
        var asArray = prototypes.ToArray();
        _categories = new(asArray.Length);
        _weights = new(asArray.Length);
        _totalWeight = 0;

        foreach (var category in asArray)
        {
            if (category.MaxItems <= 0 || category.Weight <= 0)
                continue;

            _totalWeight += category.Weight;
            _categories.Add(category);
            _weights.Add(_totalWeight);
        }
    }

    /// <summary>
    /// Removes a category from the pool and adjusts cumulative weights for all subsequent entries.
    /// </summary>
    public void Remove(T category)
    {
        var index = _categories.IndexOf(category);
        if (index == -1)
            return;

        for (var i = index + 1; i < _categories.Count; i++)
            _weights[i] -= category.Weight;

        _totalWeight -= category.Weight;
        _categories.RemoveAt(index);
        _weights.RemoveAt(index);
    }

    /// <summary>
    /// Rolls a random category weighted by <see cref="IWeightedCategory.Weight"/>.
    /// Returns null if the pool is empty.
    /// </summary>
    public T? RollCategory(IRobustRandom random)
    {
        if (_totalWeight <= 0)
            return null;

        var roll = random.Next(_totalWeight);
        for (var i = 0; i < _weights.Count; i++)
        {
            if (roll < _weights[i])
                return _categories[i];
        }

        return null;
    }
}
