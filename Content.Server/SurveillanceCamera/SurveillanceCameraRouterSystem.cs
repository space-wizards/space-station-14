using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Content.Shared.SurveillanceCamera;
using Robust.Shared.Prototypes;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraRouterSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraRouterComponent, ComponentInit>(OnInitialize);
        SubscribeLocalEvent<SurveillanceCameraRouterComponent, DeviceNetworkPacketEvent>(OnPacketReceive);
    }

    private void OnInitialize(EntityUid uid, SurveillanceCameraRouterComponent router, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(router.SubnetFrequencyId, out DeviceFrequencyPrototype? subnetFrequency))
        {
            return;
        }

        router.SubnetFrequency = subnetFrequency.Frequency;
    }

    private void OnPacketReceive(EntityUid uid, SurveillanceCameraRouterComponent router, DeviceNetworkPacketEvent args)
    {
        if (string.IsNullOrEmpty(args.SenderAddress)
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
            case SurveillanceCameraSystem.CameraHeartbeatMessage:
                if (!args.Data.TryGetValue(SurveillanceCameraSystem.CameraAddressData, out string? camera))
                {
                    return;
                }

                SendHeartbeat(uid, args.SenderAddress, camera, router);
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

    private void SendHeartbeat(EntityUid uid, string origin, string destination,
        SurveillanceCameraRouterComponent? router = null)
    {
        if (!Resolve(uid, ref router))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraHeartbeatMessage },
            { SurveillanceCameraSystem.CameraAddressData, origin }
        };

        _deviceNetworkSystem.QueuePacket(uid, destination, payload, router.SubnetFrequency);
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
            { DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraPingMessage },
            { SurveillanceCameraSystem.CameraSubnetData, router.SubnetName }
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
            _deviceNetworkSystem.QueuePacket(uid, address, payload);
        }
    }
}
