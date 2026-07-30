using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Payloads;
using Content.Shared.SurveillanceCamera;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
/// A system for re-routing <see cref="IRoutableNetworkPayload"/>s
/// through an entity with <see cref="DeviceNetworkRouterComponent"/>.
/// </summary>
public sealed partial class DeviceNetworkRouterSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;

    [Dependency] private EntityQuery<DeviceNetworkComponent> _query = default!;

    // TODO: make an engine PR to allow for auto-generated relay subscriptions
    // Should be doable by using reflection on marker interfaces and then adding them to the auto-generated subscriptions
    // I know it looks absolutely hilarious and horrible, but uuuhhhh anything to not make boxing allocations!!!!!!!! :godo:
    public override void Initialize()
    {
        base.Initialize();

        // Surveillance cameras
        SubscribeLocalEvent<DeviceNetworkRouterComponent, DeviceNetworkPacketEvent<RoutedNetworkPayload<SurveillanceCameraConnectPayload>>>((ent, ref args) => OnRoutePayload(ent, ref args));
        SubscribeLocalEvent<DeviceNetworkRouterComponent, DeviceNetworkPacketEvent<RoutedNetworkPayload<SurveillanceCameraConnectRequestPayload>>>((ent, ref args) => OnRoutePayload(ent, ref args));
        SubscribeLocalEvent<DeviceNetworkRouterComponent, DeviceNetworkPacketEvent<RoutedNetworkPayload<SurveillanceCameraHeartbeatRequestPayload>>>((ent, ref args) => OnRoutePayload(ent, ref args));
        SubscribeLocalEvent<DeviceNetworkRouterComponent, DeviceNetworkPacketEvent<RoutedNetworkPayload<SurveillanceCameraHeartbeatPayload>>>((ent, ref args) => OnRoutePayload(ent, ref args));
        SubscribeLocalEvent<DeviceNetworkRouterComponent, DeviceNetworkPacketEvent<RoutedNetworkPayload<SurveillanceCameraPingPayload>>>((ent, ref args) => OnRoutePayload(ent, ref args));
        SubscribeLocalEvent<DeviceNetworkRouterComponent, DeviceNetworkPacketEvent<RoutedNetworkPayload<SurveillanceCameraDataPayload>>>((ent, ref args) => OnRoutePayload(ent, ref args));
    }

    private void OnRoutePayload<T>(Entity<DeviceNetworkRouterComponent> ent, ref DeviceNetworkPacketEvent<T> args) where T : IRoutedNetworkPayload
    {
        var payload = args.Data;
        if (!_query.TryComp(ent, out var deviceComp))
            return;

        args.Data.Reroute(ent.Owner,
            payload.TargetAddress,
            payload.OverrideFrequency ?? deviceComp.Data.TransmitFrequency,
            payload.OverrideNetwork ?? deviceComp.DeviceNetId,
            _deviceNetworkSystem);
    }

    /// <summary>
    /// Sends the given <see cref="IRoutableNetworkPayload"/> as a device network packet to the Relay entity with the given address and frequency.
    /// After the payload is received by an entity with <see cref="DeviceNetworkRouterComponent"/>,
    /// it gets re-routed to <see cref="targetAddress"/>.
    /// </summary>
    /// <remarks>
    /// This is useful for routing server setups where the packet must be sent in both directions through a server.
    /// </remarks>
    /// <param name="ent">The sending entity.</param>
    /// <param name="data">The data to be sent.</param>
    /// <param name="routerAddress">
    /// The address of the entity with <see cref="DeviceNetworkRouterComponent"/> that the packet gets sent to.
    /// If null, the message is broadcast to all devices on that frequency (except the sender).
    /// This address must include the router entity, so the packet can be relayed.
    /// </param>
    /// <param name="targetAddress">
    /// The Actual address to which the payload will be relayed
    /// to after it was received by a <see cref="DeviceNetworkRouterComponent"/>.
    /// </param>
    /// <param name="frequency">The frequency to send on to the router.</param>
    /// <param name="overrideFrequency">If specified, will use this frequency when re-routing the packet.</param>
    /// <param name="network">The network to send on to the router.</param>
    /// <param name="overrideNetwork">If specified, will use this network when re-routing the packet.</param>
    /// <returns>Returns true when the packet was successfully enqueued.</returns>
    public void QueuePacketRouted<T>(
        Entity<DeviceNetworkComponent?> ent,
        ref T data,
        string? routerAddress,
        string? targetAddress,
        uint? overrideFrequency = null,
        uint? frequency = null,
        int? overrideNetwork = null,
        int? network = null)
        where T : IRoutableNetworkPayload
    {
        if (!_query.Resolve(ref ent) || ent.Comp == null)
            return;

        data.SenderAddress = ent.Comp.Data.Address;
        data.Sender = ent.Owner;
        var payload = new RoutedNetworkPayload<T>
        {
            Payload = data,
            OverrideFrequency = overrideFrequency,
            OverrideNetwork = overrideNetwork,
            TargetAddress = targetAddress,
        };

        _deviceNetworkSystem.QueuePacket(ent.Owner, routerAddress, ref payload, frequency, network);
    }
}
