using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Payloads;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkRouterSystem : DevicePayloadSystem<DeviceNetworkRouterComponent>
{
    [Dependency] private SharedDeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private EntityQuery<DeviceNetworkComponent> _query = default!;

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<RoutedNetworkPayload>(OnRoutePayload);
    }

    private void OnRoutePayload(
        Entity<DeviceNetworkRouterComponent> ent,
        ref RoutedNetworkPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (!_query.TryComp(ent, out var deviceComp))
            return;

        var frequency = deviceComp.TransmitFrequency;
        if (payload.OverrideFrequency)
            frequency = ent.Comp.TransmitFrequency ?? deviceComp.TransmitFrequency;

        _deviceNetworkSystem.QueuePacket(
            ent.Owner,
            payload.TargetAddress,
            payload.Payload,
            frequency);
    }

    public void QueuePacketRouted(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        RoutableNetworkPayload data,
        string? targetAddress,
        bool overrideFrequency = false,
        uint? frequency = null,
        int? network = null)
    {
        if (!_query.Resolve(ref ent) || ent.Comp == null)
            return;

        data.SenderAddress = ent.Comp.Address;
        data.Sender = GetNetEntity(ent.Owner);
        var payload = new RoutedNetworkPayload
        {
            Payload = data,
            OverrideFrequency = overrideFrequency,
            TargetAddress = targetAddress,
        };

        _deviceNetworkSystem.QueuePacket(ent.Owner, address, payload, frequency, network);
    }
}
