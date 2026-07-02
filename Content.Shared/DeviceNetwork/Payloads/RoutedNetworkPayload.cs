using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork.Payloads;

/// <summary>
/// Represents a payload that can be re-routed by a <see cref="DeviceNetworkRouterComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RoutedNetworkPayload : HandledNetworkPayload
{
    [DataField]
    public RoutableNetworkPayload Payload;

    /// <summary>
    /// If true, the device router will try to use a different frequency for transmitting this packet.
    /// </summary>
    [DataField]
    public bool OverrideFrequency;

    /// <summary>
    /// Address to re-route to when the <see cref="RoutedNetworkPayload"/> is being handled.
    /// </summary>
    [DataField]
    public string? TargetAddress;
}

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class RoutableNetworkPayload : HandledNetworkPayload
{
    /// <summary>
    /// Original sender address, before the packet was re-routed.
    /// </summary>
    [DataField]
    public string? SenderAddress;

    [DataField]
    public NetEntity Sender;
}
