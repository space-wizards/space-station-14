using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared.ValueSelectors.Floats;

/// <summary>
/// Used for implementing custom value selection.
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class FloatSelector
{
    /// <summary>
    /// Returns a value for this selector given a randomizer.
    /// </summary>
    public abstract float Get(IRobustRandom rand);

    /// <summary>
    /// Odds of occurrence
    /// </summary>
    /// <returns>An odds multiplier of at least one occurrence</returns>
    public abstract float Odds();

    /// <summary>
    /// Average number of occurrences
    /// </summary>
    /// <returns>The average amount of occurrences</returns>
    public abstract float Average();
}
