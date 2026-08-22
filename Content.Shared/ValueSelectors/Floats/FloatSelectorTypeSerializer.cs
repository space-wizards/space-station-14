using System.Numerics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.ValueSelectors.Floats;

[TypeSerializer]
public sealed class FloatSelectorTypeSerializer :
    BaseValueSelectorTypeSerializer<float, float, FloatSelector>,
    ITypeReader<FloatSelector, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return ValidateImpl(serializationManager, node, dependencies, context);
    }

    public FloatSelector Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<FloatSelector>? instanceProvider = null)
    {
        return ReadImpl(serializationManager, node, dependencies, hookCtx, context, instanceProvider);
    }

    protected override FloatSelector GetConstantSelector(float constant)
    {
        return new ConstantFloatSelector(constant);
    }

    protected override FloatSelector GetRangeSelector(float min, float max)
    {
        return new RangeFloatSelector(new Vector2(min, max));
    }
}
