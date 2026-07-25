namespace Content.Shared.ValueSelectors.Numbers;

[TypeSerializer]
public sealed class NumberSelectorTypeSerializer : BaseValueSelectorTypeSerializer<int, float>
{
    protected override IBaseValueSelector<int, float> GetConstantSelector(int constant)
    {
        return new ConstantNumberSelector(constant);
    }

    protected override IBaseValueSelector<int, float> GetRangeSelector(int min, int max)
    {
        return new RangeNumberSelector(new Vector2i(min, max));
    }
}
