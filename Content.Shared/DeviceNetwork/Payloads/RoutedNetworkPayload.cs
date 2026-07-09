using Content.Shared.DeviceNetwork.Components;
namespace Content.Shared.DeviceNetwork.Payloads;

/// <summary>
/// Represents a payload that can be re-routed by a <see cref="DeviceNetworkRouterComponent"/>.
/// </summary>
public sealed partial class RoutedNetworkPayload : NetworkPayloadBase<RoutedNetworkPayload>
{
    [DataField]
    public IRoutableNetworkPayload Payload;

    /// <summary>
    /// If specified, the device router will use this frequency for transmitting the <see cref="Payload"/>.
    /// </summary>
    [DataField]
    public uint? OverrideFrequency;

    /// <summary>
    /// If specified, the device router will use this network ID for transmitting the <see cref="Payload"/>.
    /// </summary>
    [DataField]
    public int? OverrideNetwork;

    /// <summary>
    /// Address to re-route to when the <see cref="RoutedNetworkPayload"/> is being handled.
    /// </summary>
    [DataField]
    public string? TargetAddress;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class RoutableNetworkPayload<T> : NetworkPayloadBase<T>, IRoutableNetworkPayload where T : NetworkPayloadBase<T>
{
    /// <summary>
    /// Original sender address, before the packet was re-routed.
    /// </summary>
    [DataField]
    public string? SenderAddress { get; set; }

    [DataField]
    public NetEntity Sender { get; set; }
}

public interface IRoutableNetworkPayload : INetworkPayload
{
    string? SenderAddress { get; set; }

    NetEntity Sender { get; set; }
}
