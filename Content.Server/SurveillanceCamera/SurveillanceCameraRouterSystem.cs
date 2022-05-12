using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.SurveillanceCamera;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraRouterSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraRouterComponent, DeviceNetworkPacketEvent>(OnPacketReceive);
    }

    private void OnPacketReceive(EntityUid uid, SurveillanceCameraRouterComponent router, DeviceNetworkPacketEvent args)
    {
        if (args.Address == null
            || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            return;
        }

        switch (command)
        {
            case SurveillanceCameraSystem.CameraConnectMessage:
                if (!args.Data.TryGetValue(SurveillanceCameraSystem.CameraAddressData, out string? address))
                {
                    return;
                }

                ConnectCamera(uid, args.SenderAddress, address, router);
                break;
            case SurveillanceCameraSystem.CameraSubnetConnectMessage:
                AddMonitorToRoute(uid, args.SenderAddress, router);
                PingSubnet(uid, router);
                break;
            case SurveillanceCameraSystem.CameraSubnetDisconnectMessage:
                RemoveMonitorFromRoute(uid, args.SenderAddress, router);
                break;
            case SurveillanceCameraSystem.CameraPingSubnetMessage:
                PingSubnet(uid, router);
                break;
            case SurveillanceCameraSystem.CameraPingMessage:
                SubnetPingResponse(uid, args.SenderAddress, router);
                break;
            case SurveillanceCameraSystem.CameraDataMessage:
                SendCameraInfo(uid, args.Data, router);
                break;
        }
    }

    private void SubnetPingResponse(EntityUid uid, string origin, SurveillanceCameraRouterComponent? router = null)
    {
        if (!Resolve(uid, ref router))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraSubnetData },
            { SurveillanceCameraSystem.CameraSubnetData, router.SubnetName }
        };

        _deviceNetworkSystem.QueuePacket(uid, origin, payload);
    }

    private void ConnectCamera(EntityUid uid, string origin, string address, SurveillanceCameraRouterComponent? router = null)
    {
        if (!Resolve(uid, ref router))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraConnectMessage },
            { SurveillanceCameraSystem.CameraAddressData, origin }
        };

        _deviceNetworkSystem.QueuePacket(uid, address, payload, router.SubnetFrequency);
    }

    // Adds a monitor to the set of routes.
    private void AddMonitorToRoute(EntityUid uid, string address, SurveillanceCameraRouterComponent? router = null)
    {
        if (!Resolve(uid, ref router))
        {
            return;
        }

        router.MonitorRoutes.Add(address);
    }

    private void RemoveMonitorFromRoute(EntityUid uid, string address, SurveillanceCameraRouterComponent? router = null)
    {
        if (!Resolve(uid, ref router))
        {
            return;
        }

        router.MonitorRoutes.Remove(address);
    }

    // Pings a subnet to get all camera information.
    private void PingSubnet(EntityUid uid, SurveillanceCameraRouterComponent? router = null)
    {
        if (!Resolve(uid, ref router))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraPingMessage }
        };

        _deviceNetworkSystem.QueuePacket(uid, null, payload, router.SubnetFrequency);
    }

    // Sends camera information to all monitors currently interested.
    private void SendCameraInfo(EntityUid uid, NetworkPayload payload, SurveillanceCameraRouterComponent? router = null)
    {
        if (!Resolve(uid, ref router))
        {
            return;
        }

        foreach (var address in router.MonitorRoutes)
        {
            _deviceNetworkSystem.QueuePacket(uid, address, payload, router.MonitorFrequency);
        }
    }
}
