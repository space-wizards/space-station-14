using Robust.Shared.Random;

namespace Content.Shared.ValueSelectors.Floats;

/// <inheritdoc cref="IConstantValueSelector{T}"/>
public sealed partial class ConstantFloatSelector : FloatSelector, IConstantValueSelector<float>
{
    [DataField]
    public float Value { get; set; } = 1f;

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
