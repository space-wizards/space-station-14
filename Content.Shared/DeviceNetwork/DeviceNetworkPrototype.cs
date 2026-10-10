using Content.Shared.DeviceNetwork.Components.Networks;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// Wrapper prototype for a device network manager.
/// </summary>
[Prototype]
public sealed partial class DeviceNetworkPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Controls actual behavior of this device network.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<DeviceNetworkManagerComponent> ManagerId;

    /// <summary>
    /// Displayed name of this device network.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;
}
