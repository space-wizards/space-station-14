using System.Numerics;

namespace Content.Shared.ValueSelectors;

/// <summary>
/// Gives a constant value.
/// </summary>
public interface IConstantValueSelector<T> where T : INumber<T>
{
    /// <summary>
    /// The constant value of this selector.
    /// </summary>
    T Value { get; set; }
}

