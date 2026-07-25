using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared.ValueSelectors;

/// <summary>
/// A base class for all selectors that return a number.
/// Used for implementing custom value selection.
/// </summary>
/// <typeparam name="TMain">Type of the main number this selector returns.</typeparam>
/// <typeparam name="TFrac">Type of the fraction this selector returns.</typeparam>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public partial interface IBaseValueSelector<out TMain, out TFrac>
    where TMain : INumber<TMain>, IParsable<TMain>
    where TFrac : INumber<TFrac>, IParsable<TFrac>
{
    /// <summary>
    /// Returns a value for this selector given a randomizer.
    /// </summary>
    TMain Get(IRobustRandom rand);

    /// <summary>
    /// Odds of occurrence.
    /// </summary>
    /// <returns>An odds multiplier of at least one occurrence.</returns>
    TFrac Odds();

    /// <summary>
    /// Average number of occurrences.
    /// </summary>
    /// <returns>The average amount of occurrences.</returns>
    TFrac Average();
}
