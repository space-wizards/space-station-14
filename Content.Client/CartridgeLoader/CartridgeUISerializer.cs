using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Client.CartridgeLoader;

/// <summary>
/// Boilerplate serializer for defining the ui fragment used for a cartridge in yaml
/// </summary>
/// <example>
/// This is an example from the yaml definition from the notekeeper ui
/// <code>
/// - type: CartridgeUi
///     ui: !type:NotekeeperUi
/// </code>
/// </example>
public sealed class CartridgeUISerializer : ITypeSerializer<CartridgeUI, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<CartridgeUI>(node, context);
    }

    public CartridgeUI Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, CartridgeUI? value = default)
    {
        return serializationManager.Read(node, hookCtx, context, value);
    }

    public CartridgeUI Copy(ISerializationManager serializationManager, CartridgeUI source, CartridgeUI target,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return serializationManager.Copy(source, hookCtx, context);
    }

    public DataNode Write(ISerializationManager serializationManager, CartridgeUI value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return serializationManager.WriteValue(value, alwaysWrite, context);
    }
}
