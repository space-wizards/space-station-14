using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.DeviceNetwork.Serializers;

[TypeSerializer]
public sealed partial class DeviceFrequencyTypeSerializer : ITypeReader<DeviceFrequency, ValueDataNode>
{
    [Dependency] private IPrototypeManager _protoMan = default!;

    public DeviceFrequency Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DeviceFrequency>? instanceProvider = null)
    {
        // A normal ushort value
        if (ushort.TryParse(node.Value, out var value))
            return new DeviceFrequency(value);

        // A double value with a decimal, serialized as being without it and cast back to ushort
        if (double.TryParse(node.Value, out var dec))
            return new DeviceFrequency((ushort) FixedPoint2.New(dec).Value);

        if (_protoMan.TryIndex<DeviceFrequencyPrototype>(node.Value, out var proto))
            return new DeviceFrequency(proto.Frequency);

        throw new InvalidMappingException($"{nameof(DeviceFrequency)} value must be parsable to ushort,  or a {nameof(DeviceFrequencyPrototype)} ID!");
    }

    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (ushort.TryParse(node.Value, out _)
            || double.TryParse(node.Value, out var dec)
            && dec < ushort.MaxValue
            || _protoMan.HasIndex<DeviceFrequencyPrototype>(node.Value))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"{nameof(DeviceFrequency)} value must be parsable to ushort or a {nameof(DeviceFrequencyPrototype)} ID!");
    }
}
