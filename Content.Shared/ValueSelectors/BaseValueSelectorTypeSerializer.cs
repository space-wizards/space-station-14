using System.Globalization;
using System.Numerics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Shared.ValueSelectors;

/// <summary>
/// Base serializer for all types implementing <see cref="IBaseValueSelector{TMain,TFrac}"/>.
/// </summary>
public abstract class BaseValueSelectorTypeSerializer<TMain, TFrac>
    where TMain : INumber<TMain>, IParsable<TMain>
    where TFrac : INumber<TFrac>, IParsable<TFrac>
{
    // Abstract methods used for creation because IDynamicTypeFactory will be slower since it's not compile-time
    // And alternative solutions probably require using methods that are outside of sandbox.
    protected abstract IBaseValueSelector<TMain, TFrac> GetConstantSelector(TMain constant);

    protected abstract IBaseValueSelector<TMain, TFrac> GetRangeSelector(TMain min, TMain max);

    protected ValidationNode ValidateImpl(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        // Constant selector validation
        if (TMain.TryParse(node.Value, CultureInfo.InvariantCulture, out _))
            return new ValidatedValueNode(node);

        // Range selector validation
        if (VectorSerializerUtility.TryParseArgs(node.Value, 2, out var args))
        {
            if (!TMain.TryParse(args[0], CultureInfo.InvariantCulture, out _)
                || !TMain.TryParse(args[1], CultureInfo.InvariantCulture, out _))
                return new ErrorNode(node, "Failed to validate a range value selector - one of the arguments can't be parsed correctly!");

            return new ValidatedValueNode(node);
        }

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    protected IBaseValueSelector<TMain, TFrac> ReadImpl(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<IBaseValueSelector<TMain, TFrac>>? instanceProvider = null)
    {
        if (TMain.TryParse(node.Value, CultureInfo.InvariantCulture, out var result))
            return GetConstantSelector(result);

        if (VectorSerializerUtility.TryParseArgs(node.Value, 2, out var args))
        {
            var x = TMain.Parse(args[0], CultureInfo.InvariantCulture);
            var y = TMain.Parse(args[1], CultureInfo.InvariantCulture);
            return GetRangeSelector(x, y);
        }

        return (IBaseValueSelector<TMain, TFrac>) serializationManager.Read(typeof(IBaseValueSelector<TMain, TFrac>), node, context)!;
    }
}
