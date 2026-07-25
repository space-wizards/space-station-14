using Robust.Shared.Random;

namespace Content.Shared.ValueSelectors.Numbers;

/// <summary>
/// Used for implementing custom value selection of <see cref="int"/>egers.
/// </summary>
public abstract partial class NumberSelector : IBaseValueSelector<int, float>
{
    /// <summary>
    /// Returns a value for this selector given a randomizer.
    /// </summary>
    public abstract int Get(IRobustRandom rand);

    /// <summary>
    /// Odds of occurrence.
    /// </summary>
    /// <returns>An odds multiplier of at least one occurrence.</returns>
    public abstract float Odds();

    /// <summary>
    /// Average number of occurrences.
    /// </summary>
    /// <returns>The average amount of occurrences.</returns>
    public abstract float Average();
}
