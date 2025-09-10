namespace Content.Shared.EntityTable.ValueSelector;

/// <summary>
/// Gives a value between the two numbers specified, inclusive.
/// </summary>
public sealed partial class RangeNumberSelector : NumberSelector
{
    [DataField]
    public Vector2i Range = new(1, 1);

    public RangeNumberSelector(Vector2i range)
    {
        Range = range;
    }

    public override int Get(System.Random rand)
    {
        // rand.Next() is inclusive on the first number and exclusive on the second number,
        // so we add 1 to the second number.
        return rand.Next(Range.X, Range.Y + 1);
    }
}
