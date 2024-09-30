using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.DeviceLinking;

/// <summary>
///     A prototype for a device port, for use with device linking.
/// </summary>
[Serializable, NetSerializable]
public abstract class DevicePortPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Localization string for the port name. Displayed in the linking UI.
    /// </summary>
    [DataField("name", required:true)]
    public string Name = default!;

    /// <summary>
    ///     Localization string for a description of the ports functionality. Should either indicate when a source
    ///     port is fired, or what function a sink port serves. Displayed as a tooltip in the linking UI.
    /// </summary>
    [DataField("description", required: true)]
    public string Description = default!;
}

[Prototype("sinkPort")]
[Serializable, NetSerializable]
public sealed partial class SinkPortPrototype : DevicePortPrototype, IPrototype
{
}

[Prototype("sourcePort")]
[Serializable, NetSerializable]
public sealed partial class SourcePortPrototype : DevicePortPrototype, IPrototype
{
    /// <summary>
    ///     This is a set of sink ports that this source port will attempt to link to when using the
    ///     default-link functionality.
    /// </summary>
    [DataField("defaultLinks", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SinkPortPrototype>))]
    public HashSet<string>? DefaultLinks;
}
