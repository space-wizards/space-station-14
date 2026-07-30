using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Shared.DeviceNetwork.Payloads;

/// <summary>
/// Represents a payload that can be re-routed by a <see cref="DeviceNetworkRouterComponent"/>.
/// </summary>
public partial interface IRoutableNetworkPayload : INetworkPayload
{
    /// <summary>
    /// Original sender address of this payload.
    /// </summary>
    DeviceAddress? SenderAddress { get; set; }

    /// <summary>
    /// Original sender entity of this payload.
    /// </summary>
    EntityUid Sender { get; set; }
}

public partial interface IRoutedNetworkPayload : INetworkPayload
{
    /// <summary>
    /// If specified, the device router will use this frequency for transmitting the <see cref="Payload"/>.
    /// </summary>
    DeviceFrequency? OverrideFrequency { get; set; }

    /// <summary>
    /// If specified, the device router will use this network ID for transmitting the <see cref="Payload"/>.
    /// </summary>
    int? OverrideNetwork { get; set; }

    /// <summary>
    /// Address to re-route to when the <see cref="RoutedNetworkPayload{T}"/> is being handled.
    /// </summary>
    DeviceAddress? TargetAddress { get; set; }

    /// <summary>
    ///
    /// </summary>
    void Reroute(EntityUid sender, DeviceAddress? address, DeviceFrequency? frequency, int? network, DeviceNetworkSystem system);
}

/// <summary>
/// A wrapper around the <see cref="IRoutableNetworkPayload"/>, sent to an entity with <see cref="DeviceNetworkRouterComponent"/>.
/// </summary>
public partial record struct RoutedNetworkPayload<T> : IRoutedNetworkPayload where T : IRoutableNetworkPayload
{
    /// <summary>
    /// The wrapped payload that is going to be sent when received by <see cref="DeviceNetworkRouterComponent"/>.
    /// </summary>
    [DataField]
    public T Payload;

    /// <summary>
    /// If specified, the device router will use this frequency for transmitting the <see cref="Payload"/>.
    /// </summary>
    [DataField]
    public DeviceFrequency? OverrideFrequency { get; set; }

    /// <summary>
    /// If specified, the device router will use this network ID for transmitting the <see cref="Payload"/>.
    /// </summary>
    [DataField]
    public int? OverrideNetwork { get; set; }

    /// <summary>
    /// Address to re-route to when the <see cref="RoutedNetworkPayload{T}"/> is being handled.
    /// </summary>
    [DataField]
    public DeviceAddress? TargetAddress { get; set; }

    public void Reroute(EntityUid sender,
        DeviceAddress? address,
        DeviceFrequency? frequency,
        int? network,
        DeviceNetworkSystem system)
    {
        // Things sometimes take a **weird route** when it comes to type parameters.
        system.QueuePacket(
            sender,
            address,
            ref Payload,
            frequency,
            network);
    }
}
