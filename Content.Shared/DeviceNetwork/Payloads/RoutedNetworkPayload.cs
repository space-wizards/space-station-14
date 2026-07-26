using Content.Shared.DeviceNetwork.Components;
namespace Content.Shared.DeviceNetwork.Payloads;

/// <summary>
/// Represents a payload that can be re-routed by a <see cref="DeviceNetworkRouterComponent"/>.
/// </summary>
public interface IRoutableNetworkPayload : INetworkPayload
{
    /// <summary>
    /// Original sender address of this payload.
    /// </summary>
    string? SenderAddress { get; set; }

    /// <summary>
    /// Original sender entity of this payload.
    /// </summary>
    EntityUid Sender { get; set; }
}

/// <inheritdoc cref="IRoutableNetworkPayload"/>
[ImplicitDataDefinitionForInheritors]
public abstract partial class RoutableNetworkPayload<T> : NetworkPayloadBase<T>, IRoutableNetworkPayload where T : NetworkPayloadBase<T>
{
    [DataField]
    public string? SenderAddress { get; set; }

    [DataField]
    public EntityUid Sender { get; set; }
}

/// <summary>
/// A wrapper around the <see cref="IRoutableNetworkPayload"/>, sent to an entity with <see cref="DeviceNetworkRouterComponent"/>.
/// </summary>
public sealed partial class RoutedNetworkPayload : NetworkPayloadBase<RoutedNetworkPayload>
{
    /// <summary>
    /// The wrapped payload that is going to be sent when received by <see cref="DeviceNetworkRouterComponent"/>.
    /// </summary>
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
