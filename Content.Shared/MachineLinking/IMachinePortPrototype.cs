using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.MachineLinking;

/// <summary>
///     A prototype for a machine port, for use with machine linking.
/// </summary>
public interface IMachinePortPrototype
{
    /// <summary>
    ///     Localization string for the port name. Displayed in the linking UI.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Localization string for a description of the ports functionality. Should either indicate when a transmitter
    ///     port is fired, or what function a receiver port serves. Displayed as a tooltip in the linking UI.
    /// </summary>
    public string Description { get; }
}

[Prototype("receiverPort")]
public readonly record struct ReceiverPortPrototype : IMachinePortPrototype, IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <inheritdoc />
    [DataField("name", required: true)]
    public string Name { get; } = default!;

    /// <inheritdoc />
    [DataField("description", required: true)]
    public string Description { get; } = default!;
}

[Prototype("transmitterPort")]
public readonly record struct TransmitterPortPrototype : IMachinePortPrototype, IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     This is a set of receiver ports that this transmitter port will attempt to link to when using the
    ///     default-link functionality.
    /// </summary>
    [DataField("defaultLinks", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<ReceiverPortPrototype>))]
    public readonly HashSet<string>? DefaultLinks;

    /// <inheritdoc />
    [DataField("name", required: true)]
    public string Name { get; } = default!;

    /// <inheritdoc />
    [DataField("description", required: true)]
    public string Description { get; } = default!;
}
