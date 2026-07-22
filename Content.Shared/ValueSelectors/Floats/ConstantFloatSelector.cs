using Robust.Shared.Random;

namespace Content.Shared.ValueSelectors.Floats;

/// <summary>
/// Gives a constant value.
/// </summary>
public sealed partial class ConstantFloatSelector : FloatSelector
{
    /// <summary>
    /// The constant value of this selector.
    /// </summary>
    [DataField]
    public float Value = 1f;

    public ConstantFloatSelector(float value)
    {
        Value = value;
    }

    public override float Get(IRobustRandom rand)
    {
        return Value;
    }

    public override float Odds()
    {
        return Value < 1f ? 0f : 1f;
    }

    public override float Average()
    {
        return Value;
    }
}
