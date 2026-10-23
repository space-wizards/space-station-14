using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// Anm interface implemented by components that connect and disconnect the device based on certain conditions.
/// </summary>
public partial interface ILimitedDeviceComponent
{
    /// <summary>
    /// If specified, the limitations set by this component don't apply to certain networks.
    /// </summary>
    ProtoId<DeviceNetworkPrototype>[]? IgnoreNets { get; set; }
}
