using System.Numerics;
using Robust.Shared.Random;

namespace Content.Shared.ValueSelectors.Floats;

/// <summary>
/// Gives a value between the two numbers specified, inclusive.
/// </summary>
public sealed partial class RangeNumberSelector : FloatSelector
{
    /// <summary>
    /// The min and max value of this selector, both are inclusive.
    /// </summary>
    [DataField]
    public Vector2 Range = new(1f, 1f);

    public RangeNumberSelector(Vector2 range)
    {
        Range = range;
    }

    public override float Get(IRobustRandom rand)
    {
        // rand.NextFloat() is inclusive on the first number and exclusive on the second number,
        // so we add 1 to the second number.
        return rand.NextFloat(Range.X, Range.Y + 1f);
    }

    public override float Odds()
    {
        return Range.X == 0 ? 1f / (Range.Y + 1f) : 1f;
    }

    public override float Average()
    {
        return (Range.X + Range.Y) / 2f;
    }
}
