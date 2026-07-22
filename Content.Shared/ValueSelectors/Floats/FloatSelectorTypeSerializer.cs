using System.Globalization;
using System.Numerics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Shared.ValueSelectors.Floats;

[TypeSerializer]
public sealed class FloatSelectorTypeSerializer :
    ITypeReader<FloatSelector, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        // ConstantFloatSelector validation
        if (float.TryParse(node.Value, out _))
            return new ValidatedValueNode(node);

        // RangeFloatSelector validation
        if (VectorSerializerUtility.TryParseArgs(node.Value, 2, out _))
        {
            return new ValidatedValueNode(node);
        }

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    public FloatSelector Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<FloatSelector>? instanceProvider = null)
    {
        var type = typeof(FloatSelector);

        if (float.TryParse(node.Value, out var result))
            return new ConstantFloatSelector(result);

        if (VectorSerializerUtility.TryParseArgs(node.Value, 2, out var args))
        {
            var x = float.Parse(args[0], CultureInfo.InvariantCulture);
            var y = float.Parse(args[1], CultureInfo.InvariantCulture);
            return new RangeNumberSelector(new Vector2(x, y));
        }

        return (FloatSelector) serializationManager.Read(type, node, context)!;
    }
}
