using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.DeviceNetwork.Serializers;

[TypeSerializer]
public sealed class DeviceFrequencyTypeSerializer : ITypeReader<DeviceFrequency, ValueDataNode>
{
    public DeviceFrequency Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DeviceFrequency>? instanceProvider = null)
    {
        return new DeviceFrequency(ushort.Parse(node.Value));
    }

    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (ushort.TryParse(node.Value, out var port))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"{nameof(DeviceFrequency)} value must be parsable to ushort!");
    }
}
