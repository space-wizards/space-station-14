using Robust.Shared.Random;

namespace Content.Shared.ValueSelectors.Numbers;

/// <inheritdoc cref="IConstantValueSelector{T}"/>
public sealed partial class ConstantNumberSelector : NumberSelector, IConstantValueSelector<int>
{
    [DataField]
    public int Value { get; set; } = 1;

    public ConstantNumberSelector(int value)
    {
        Value = value;
    }

    public override int Get(IRobustRandom rand)
    {
        return Value;
    }

    public override float Odds()
    {
        // You really shouldn't have a constant value of 0 ever.
        return 1;
    }

    public override float Average()
    {
        return Value;
    }
}
