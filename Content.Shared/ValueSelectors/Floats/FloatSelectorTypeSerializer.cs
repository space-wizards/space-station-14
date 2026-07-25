using System.Numerics;

namespace Content.Shared.ValueSelectors.Floats;

[TypeSerializer]
public sealed class FloatSelectorTypeSerializer  : BaseValueSelectorTypeSerializer<float, float>
{
    protected override IBaseValueSelector<float, float> GetConstantSelector(float constant)
    {
        return new ConstantFloatSelector(constant);
    }

    protected override IBaseValueSelector<float, float> GetRangeSelector(float min, float max)
    {
        return new RangeFloatSelector(new Vector2(min, max));
    }
}
