using System.Globalization;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.DeviceNetwork.Serializers;

[TypeSerializer]
public sealed class DeviceAddressTypeSerializer : ITypeSerializer<DeviceAddress, ValueDataNode>
{
    public DeviceAddress Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DeviceAddress>? instanceProvider = null)
    {
        if (int.TryParse(node.Value, out var intValue))
            return new DeviceAddress(intValue);

        // Fallback for map-saved string addresses.
        var hex = node.Value.Contains('-') ? node.Value.Split('-')[1] : node.Value; // Trim the suffix
        if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
            return new DeviceAddress(hexValue);

        throw new InvalidMappingException($"{nameof(DeviceAddress)} must be an integer, or a HEX string with an optional prefix!");
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

    public DataNode Write(
        ISerializationManager serializationManager,
        DeviceAddress value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.AddressId.ToString());
    }
}
