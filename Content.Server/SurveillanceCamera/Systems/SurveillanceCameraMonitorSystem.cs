using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.UserInterface;
using Content.Shared.SurveillanceCamera;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Map;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMonitorSystem : EntitySystem
{
    [Dependency] private readonly SurveillanceCameraSystem _surveillanceCameras = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, SurveillanceCameraDeactivateEvent>(OnSurveillanceCameraDeactivate);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        Subs.BuiEvents<SurveillanceCameraMonitorComponent>(SurveillanceCameraMonitorUiKey.Key, subs =>
        {
            subs.Event<SurveillanceCameraRefreshCamerasMessage>(OnRefreshCamerasMessage);
            subs.Event<SurveillanceCameraRefreshSubnetsMessage>(OnRefreshSubnetsMessage);
            subs.Event<SurveillanceCameraDisconnectMessage>(OnDisconnectMessage);
            // subs.Event<SurveillanceCameraMonitorSubnetRequestMessage>(OnSubnetRequest);
            subs.Event<SurveillanceCameraMonitorSwitchMessage>(OnSwitchMessage);
            subs.Event<BoundUIClosedEvent>(OnBoundUiClose);
        });
    }

    private const float _maxHeartbeatTime = 300f;
    private const float _heartbeatDelay = 30f;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveSurveillanceCameraMonitorComponent, SurveillanceCameraMonitorComponent>();
        while (query.MoveNext(out var uid, out _, out var monitor))
        {
            if (Paused(uid))
            {
                continue;
            }

            monitor.LastHeartbeatSent += frameTime;
            SendHeartbeat(uid, monitor);
            monitor.LastHeartbeat += frameTime;

            if (monitor.LastHeartbeat > _maxHeartbeatTime)
            {
                DisconnectCamera(uid, true, monitor);
                EntityManager.RemoveComponent<ActiveSurveillanceCameraMonitorComponent>(uid);
            }
        }
    }

    /// ROUTING:
    ///
    /// Monitor freq: General frequency for cameras, routers, and monitors to speak on.
    ///
    /// Subnet freqs: Frequency for each specific subnet. Routers ping cameras here,
    ///               cameras ping back on monitor frequency. When a monitor
    ///               selects a subnet, it saves that subnet's frequency
    ///               so it can connect to the camera. All outbound cameras
    ///               always speak on the monitor frequency and will not
    ///               do broadcast pings - whatever talks to it, talks to it.
    ///
    /// How a camera is discovered:
    ///
    /// Subnet ping:
    /// Surveillance camera monitor - [ monitor freq ] -> Router
    /// Router -> camera discovery
    /// Router - [ subnet freq ] -> Camera
    /// Camera -> router ping
    /// Camera - [ monitor freq ] -> Router
    /// Router -> monitor data forward
    /// Router - [ monitor freq ] -> Monitor

    #region Event Handling
    private void OnComponentStartup(EntityUid uid, SurveillanceCameraMonitorComponent component, ComponentStartup args)
    {
        RefreshSubnets(uid, component);
    }

    // private void OnSubnetRequest(EntityUid uid, SurveillanceCameraMonitorComponent component,
    //     SurveillanceCameraMonitorSubnetRequestMessage args)
    // {
    //     if (args.Session.AttachedEntity != null)
    //     {
    //         SetActiveSubnet(uid, args.Subnet, component);
    //     }
    // }

    private void OnPacketReceived(EntityUid uid, SurveillanceCameraMonitorComponent component,
        DeviceNetworkPacketEvent args)
    {
        if (string.IsNullOrEmpty(args.SenderAddress))
        {
            return;
        }

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            switch (command)
            {
                case SurveillanceCameraSystem.CameraConnectMessage:
                    if (component.NextCameraAddress == args.SenderAddress)
                    {
                        component.ActiveCameraAddress = args.SenderAddress;
                        TrySwitchCameraByUid(uid, args.Sender, component);
                    }

                    component.NextCameraAddress = null;
                    break;
                case SurveillanceCameraSystem.CameraHeartbeatMessage:
                    if (args.SenderAddress == component.ActiveCameraAddress)
                    {
                        component.LastHeartbeat = 0;
                    }

                    break;
                case SurveillanceCameraSystem.CameraDataMessage:
                    if (!args.Data.TryGetValue(SurveillanceCameraSystem.CameraNameData, out string? name)
                        || !args.Data.TryGetValue(SurveillanceCameraSystem.CameraSubnetData, out string? subnetData)
                        || !args.Data.TryGetValue(SurveillanceCameraSystem.CameraAddressData, out string? address)
                        || !args.Data.TryGetValue(SurveillanceCameraSystem.CameraUid, out string? camera))
                    {
                        return;
                    }

                    // if (component.ActiveSubnet != subnetData)
                    // {
                    //     DisconnectFromSubnet(uid, subnetData);
                    // }

                    if (!component.KnownSubnets.TryGetValue(subnetData, out var subnetAddress))
                    {
                        return;
                    }

                    EntityUid cameraUid;
                    EntityUid.TryParse(camera, out cameraUid);
                    var netEntCamera = GetNetEntity(cameraUid);

                    if (!component.KnownCameras.ContainsKey(netEntCamera))
                    {
                        EntityCoordinates coordinates = EntityCoordinates.Invalid;
                        var xformQuery = GetEntityQuery<TransformComponent>();

                        if (TryComp<TransformComponent>(cameraUid, out var transform))
                        {
                            if (transform.GridUid != null)
                            {
                                coordinates = new EntityCoordinates(transform.GridUid.Value,
                                    _transform.GetInvWorldMatrix(xformQuery.GetComponent(transform.GridUid.Value), xformQuery)
                                        .Transform(_transform.GetWorldPosition(transform, xformQuery)));
                            }
                            else if (transform.MapUid != null)
                            {
                                coordinates = new EntityCoordinates(transform.MapUid.Value,
                                    _transform.GetWorldPosition(transform, xformQuery));
                            }
                        }

                        component.KnownCameras.Add(netEntCamera, new CameraData{Name = name, CameraAddress = address, SubnetAddress = subnetAddress, Coordinates = GetNetCoordinates(coordinates)});
                    }

                    UpdateUserInterface(uid, component);
                    break;
                case SurveillanceCameraSystem.CameraSubnetData:
                    if (args.Data.TryGetValue(SurveillanceCameraSystem.CameraSubnetData, out string? subnet)
                        && !component.KnownSubnets.ContainsKey(subnet))
                    {
                        component.KnownSubnets.Add(subnet, args.SenderAddress);
                    }

                    UpdateUserInterface(uid, component);
                    break;
            }
        }
    }

    private void OnDisconnectMessage(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraDisconnectMessage message)
    {
        DisconnectCamera(uid, true, component);
        foreach (var subnet in component.KnownSubnets)
        {
            DisconnectFromSubnet(uid, subnet.Key);
            component.KnownCameras.Clear();
            UpdateUserInterface(uid, component);
            ConnectToSubnet(uid, subnet.Key);
        }
    }

    private void OnRefreshCamerasMessage(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraRefreshCamerasMessage message)
    {
        foreach (var subnet in component.KnownSubnets)
        {
            DisconnectFromSubnet(uid, subnet.Key);
            DisconnectCamera(uid, true, component);
            component.KnownCameras.Clear();
            UpdateUserInterface(uid, component);
            ConnectToSubnet(uid, subnet.Key);
        }
    }

    private void OnRefreshSubnetsMessage(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraRefreshSubnetsMessage message)
    {
        RefreshSubnets(uid, component);
    }

    private void OnSwitchMessage(EntityUid uid, SurveillanceCameraMonitorComponent component, SurveillanceCameraMonitorSwitchMessage message)
    {
        // there would be a null check here, but honestly
        // whichever one is the "latest" switch message gets to
        // do the switch
        TrySwitchCameraByAddress(uid, message.CameraAddress, message.SubnetAddress, component);
    }

    private void OnPowerChanged(EntityUid uid, SurveillanceCameraMonitorComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            RemoveActiveCamera(uid, component);
            component.NextCameraAddress = null;
        }
    }

    private void OnToggleInterface(EntityUid uid, SurveillanceCameraMonitorComponent component,
        AfterActivatableUIOpenEvent args)
    {
        AfterOpenUserInterface(uid, args.User, component);
    }

    // This is to ensure that there's no delay in ensuring that a camera is deactivated.
    private void OnSurveillanceCameraDeactivate(EntityUid uid, SurveillanceCameraMonitorComponent monitor, SurveillanceCameraDeactivateEvent args)
    {
        DisconnectCamera(uid, false, monitor);
    }

    private void OnBoundUiClose(EntityUid uid, SurveillanceCameraMonitorComponent component, BoundUIClosedEvent args)
    {
        if (args.Session.AttachedEntity == null)
        {
            return;
        }

        RemoveViewer(uid, args.Session.AttachedEntity.Value, component);
    }
    #endregion

    private void SendHeartbeat(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || monitor.LastHeartbeatSent < _heartbeatDelay
            || monitor.ActiveCamera == null
            || !monitor.KnownCameras.TryGetValue(GetNetEntity(monitor.ActiveCamera.Value), out var cameraData))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraHeartbeatMessage },
            { SurveillanceCameraSystem.CameraAddressData, monitor.ActiveCameraAddress }
        };

        _deviceNetworkSystem.QueuePacket(uid, cameraData.SubnetAddress, payload);
    }

    private void DisconnectCamera(EntityUid uid, bool removeViewers, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        if (removeViewers)
        {
            RemoveActiveCamera(uid, monitor);
        }

        monitor.ActiveCamera = null;
        monitor.ActiveCameraAddress = string.Empty;
        EntityManager.RemoveComponent<ActiveSurveillanceCameraMonitorComponent>(uid);
        UpdateUserInterface(uid, monitor);
    }

    private void RefreshSubnets(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        monitor.KnownSubnets.Clear();
        PingCameraNetwork(uid, monitor);
    }

    private void PingCameraNetwork(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraPingMessage }
        };
        _deviceNetworkSystem.QueuePacket(uid, null, payload);
    }

    // private void SetActiveSubnet(EntityUid uid, string subnet,
    //     SurveillanceCameraMonitorComponent? monitor = null)
    // {
    //     if (!Resolve(uid, ref monitor)
    //         || !monitor.KnownSubnets.ContainsKey(subnet))
    //     {
    //         return;
    //     }
    //
    //     DisconnectFromSubnet(uid, monitor.ActiveSubnet);
    //     DisconnectCamera(uid, true, monitor);
    //     monitor.ActiveSubnet = subnet;
    //     monitor.KnownCameras.Clear();
    //     UpdateUserInterface(uid, monitor);
    //
    //     ConnectToSubnet(uid, subnet);
    // }

    private void RequestSubnetInfo(EntityUid uid, string subnet, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !monitor.KnownSubnets.TryGetValue(subnet, out var address))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            {DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraPingSubnetMessage},
        };
        _deviceNetworkSystem.QueuePacket(uid, address, payload);
    }

    private void ConnectToSubnet(EntityUid uid, string subnet, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !monitor.KnownSubnets.TryGetValue(subnet, out var address))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            {DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraSubnetConnectMessage},
        };
        _deviceNetworkSystem.QueuePacket(uid, address, payload);

        RequestSubnetInfo(uid, subnet, monitor);
    }

    private void DisconnectFromSubnet(EntityUid uid, string subnet, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !monitor.KnownSubnets.TryGetValue(subnet, out var address))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            {DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraSubnetDisconnectMessage},
        };
        _deviceNetworkSystem.QueuePacket(uid, address, payload);
    }

    // Adds a viewer to the camera and the monitor.
    private void AddViewer(EntityUid uid, EntityUid player, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        monitor.Viewers.Add(player);

        if (monitor.ActiveCamera != null)
        {
            _surveillanceCameras.AddActiveViewer(monitor.ActiveCamera.Value, player, uid);
        }

        UpdateUserInterface(uid, monitor, player);
    }

    // Removes a viewer from the camera and the monitor.
    private void RemoveViewer(EntityUid uid, EntityUid player, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        monitor.Viewers.Remove(player);

        if (monitor.ActiveCamera != null)
        {
            _surveillanceCameras.RemoveActiveViewer(monitor.ActiveCamera.Value, player);
        }
    }

    // Sets the camera. If the camera is not null, this will return.
    //
    // The camera should always attempt to switch over, rather than
    // directly setting it, so that the active viewer list and view
    // subscriptions can be updated.
    private void SetCamera(EntityUid uid, EntityUid camera, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || monitor.ActiveCamera != null)
        {
            return;
        }

        _surveillanceCameras.AddActiveViewers(camera, monitor.Viewers, uid);

        monitor.ActiveCamera = camera;

        AddComp<ActiveSurveillanceCameraMonitorComponent>(uid);

        UpdateUserInterface(uid, monitor);
    }

    // Switches the camera's viewers over to this new given camera.
    private void SwitchCamera(EntityUid uid, EntityUid camera, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || monitor.ActiveCamera == null)
        {
            return;
        }

        _surveillanceCameras.SwitchActiveViewers(monitor.ActiveCamera.Value, camera, monitor.Viewers, uid);

        monitor.ActiveCamera = camera;

        UpdateUserInterface(uid, monitor);
    }

    private void TrySwitchCameraByAddress(EntityUid uid, string camera, string subnet,
        SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            {DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraConnectMessage},
            {SurveillanceCameraSystem.CameraAddressData, camera}
        };

        monitor.NextCameraAddress = camera;
        _deviceNetworkSystem.QueuePacket(uid, subnet, payload);
    }

    // Attempts to switch over the current viewed camera on this monitor
    // to the new camera.
    private void TrySwitchCameraByUid(EntityUid uid, EntityUid newCamera, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        if (monitor.ActiveCamera == null)
        {
            SetCamera(uid, newCamera, monitor);
        }
        else
        {
            SwitchCamera(uid, newCamera, monitor);
        }
    }

    private void RemoveActiveCamera(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || monitor.ActiveCamera == null)
        {
            return;
        }

        _surveillanceCameras.RemoveActiveViewers(monitor.ActiveCamera.Value, monitor.Viewers, uid);

        UpdateUserInterface(uid, monitor);
    }

    // This is public primarily because it might be useful to have the ability to
    // have this component added to any entity, and have them open the BUI (somehow).
    public void AfterOpenUserInterface(EntityUid uid, EntityUid player, SurveillanceCameraMonitorComponent? monitor = null, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !Resolve(player, ref actor))
        {
            return;
        }

        AddViewer(uid, player);
    }

    private void UpdateUserInterface(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null, EntityUid? player = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        var state = new SurveillanceCameraMonitorUiState(GetNetEntity(monitor.ActiveCamera), monitor.KnownSubnets.Keys.ToHashSet(), monitor.ActiveCameraAddress, monitor.KnownCameras);
        _userInterface.TrySetUiState(uid, SurveillanceCameraMonitorUiKey.Key, state);
    }
}
