using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.ValueSelectors.Numbers;

[TypeSerializer]
public sealed class NumberSelectorTypeSerializer :
    BaseValueSelectorTypeSerializer<int, float>,
    ITypeReader<NumberSelector, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return ValidateImpl(serializationManager, node, dependencies, context);
    }

    public NumberSelector Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<NumberSelector>? instanceProvider = null)
    {
        return (NumberSelector) ReadImpl(serializationManager, node, dependencies, hookCtx, context, instanceProvider);
    }

    protected override IBaseValueSelector<int, float> GetConstantSelector(int constant)
    {
        return new ConstantNumberSelector(constant);
    }

    protected override IBaseValueSelector<int, float> GetRangeSelector(int min, int max)
    {
        return new RangeNumberSelector(new Vector2i(min, max));
    }
}
