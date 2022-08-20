using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Client.CartridgeLoader;

public sealed class CartridgeUISerializer : ITypeSerializer<CartridgeUI, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<CartridgeUI>(node, context);
    }

    public CartridgeUI Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, CartridgeUI? value = default)
    {
        return serializationManager.Read(node, context, skipHook, value);
    }

    public DataNode Write(ISerializationManager serializationManager, CartridgeUI value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return serializationManager.WriteValue(value, alwaysWrite, context);
    }

    public CartridgeUI Copy(ISerializationManager serializationManager, CartridgeUI source, CartridgeUI target, bool skipHook,
        ISerializationContext? context = null)
    {
        return serializationManager.Copy(source, context, skipHook);
    }
}
