using Robust.Shared.Random;

namespace Content.Shared.EntityTable.ValueSelector;

/// <summary>
/// Gives a constant value.
/// </summary>
public sealed partial class ConstantNumberSelector : NumberSelector
{
    [DataField]
    public int Value = 1;

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
