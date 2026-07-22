using System.Numerics;
using Robust.Shared.Random;

namespace Content.Shared.ValueSelectors.Floats;

/// <summary>
/// Gives a value between the two floats specified, inclusive.
/// </summary>
public sealed partial class RangeFloatSelector : FloatSelector
{
    /// <summary>
    /// The min and max value of this selector, both are inclusive.
    /// </summary>
    [DataField]
    public Vector2 Range = new(1f, 1f);

    public RangeFloatSelector(Vector2 range)
    {
        Range = range;
    }

    public override float Get(IRobustRandom rand)
    {
        // rand.NextFloat() is inclusive on the first number and exclusive on the second number,
        // so we increment it by a single bit to also include the next floating point.
        return rand.NextFloat(Range.X, MathF.BitIncrement(Range.Y));
    }

    public override float Odds()
    {
        if (Range.Y < 1f)
            return 0f;

        if (Range.X >= 1f)
            return 1f;

        return (Range.Y - 1f) / (Range.Y - Range.X);
    }

    public override float Average()
    {
        return (Range.X + Range.Y) / 2f;
    }
}
