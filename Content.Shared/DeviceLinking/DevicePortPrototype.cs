using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking;

/// <summary>
///     A prototype for a device port, for use with device linking.
/// </summary>
[DataDefinition]
public abstract partial class DevicePortPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Localization string for the port name. Displayed in the linking UI.
    /// </summary>
    [DataField(required:true)]
    public LocId Name;

    /// <summary>
    ///     Localization string for a description of the ports functionality. Should either indicate when a source
    ///     port is fired, or what function a sink port serves. Displayed as a tooltip in the linking UI.
    /// </summary>
    [DataField(required: true)]
    public LocId Description;
}

[Prototype]
public sealed partial class SinkPortPrototype : DevicePortPrototype, IPrototype;

[Prototype]
public sealed partial class SourcePortPrototype : DevicePortPrototype, IPrototype
{
    /// <summary>
    ///     This is a set of sink ports that this source port will attempt to link to when using the
    ///     default-link functionality.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SinkPortPrototype>>? DefaultLinks;
}
