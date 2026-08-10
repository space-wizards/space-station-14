using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.DeviceNetwork.Serializers;

[TypeSerializer]
public sealed class DeviceAddressTypeSerializer : ITypeReader<DeviceAddress, ValueDataNode>
{
    public DeviceAddress Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DeviceAddress>? instanceProvider = null)
    {
        return new DeviceAddress(int.Parse(node.Value));
    }

    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (int.TryParse(node.Value, out _))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"{nameof(DeviceAddress)} value must be parsable to int!");
    }
}
