using System.Linq;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Content.Shared.SurveillanceCamera;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraMonitorSystem : DevicePayloadSystem<SurveillanceCameraMonitorComponent>
{
    [Dependency] private SurveillanceCameraSystem _surveillanceCameras = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private DeviceNetworkRouterSystem _deviceNetworkRouter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, SurveillanceCameraDeactivateEvent>(OnSurveillanceCameraDeactivate);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        Subs.BuiEvents<SurveillanceCameraMonitorComponent>(SurveillanceCameraMonitorUiKey.Key, subs =>
        {
            subs.Event<SurveillanceCameraRefreshCamerasMessage>(OnRefreshCamerasMessage);
            subs.Event<SurveillanceCameraRefreshSubnetsMessage>(OnRefreshSubnetsMessage);
            subs.Event<SurveillanceCameraDisconnectMessage>(OnDisconnectMessage);
            subs.Event<SurveillanceCameraMonitorSubnetRequestMessage>(OnSubnetRequest);
            subs.Event<SurveillanceCameraMonitorSwitchMessage>(OnSwitchMessage);
            subs.Event<BoundUIClosedEvent>(OnBoundUiClose);
        });
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<SurveillanceCameraConnectPayload>(OnCameraConnect);
        SubscribePayload<SurveillanceCameraHeartbeatPayload>(OnCameraHeartbeat);
        SubscribePayload<SurveillanceCameraDataPayload>(OnCameraData);
        SubscribePayload<SurveillanceCameraSubnetDataPayload>(OnSubnetData);
    }

    private const float _maxHeartbeatTime = 300f;
    private const float _heartbeatDelay = 30f;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveSurveillanceCameraMonitorComponent, SurveillanceCameraMonitorComponent>();
        while (query.MoveNext(out var uid, out _, out var monitor))
        {
            monitor.LastHeartbeatSent += frameTime;
            SendHeartbeat(uid, monitor);
            monitor.LastHeartbeat += frameTime;

            if (monitor.LastHeartbeat > _maxHeartbeatTime)
            {
                DisconnectCamera(uid, true, monitor);
                RemComp<ActiveSurveillanceCameraMonitorComponent>(uid);
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

    private void OnSubnetRequest(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraMonitorSubnetRequestMessage args)
    {
        if (args.Actor is { Valid: true } actor && !Deleted(actor))
        {
            SetActiveSubnet(uid, args.Subnet, component);
        }
    }

    private void OnCameraConnect(
        Entity<SurveillanceCameraMonitorComponent> ent,
        ref SurveillanceCameraConnectPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (ent.Comp.NextCameraAddress == payload.SenderAddress)
        {
            if (payload.SenderAddress != null)
                ent.Comp.ActiveCameraAddress = payload.SenderAddress;
            TrySwitchCameraByUid(ent, GetEntity(payload.Sender), ent.Comp);
        }

        ent.Comp.NextCameraAddress = null;
    }

    private void OnCameraHeartbeat(
        Entity<SurveillanceCameraMonitorComponent> ent,
        ref SurveillanceCameraHeartbeatPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (payload.SenderAddress == ent.Comp.ActiveCameraAddress)
        {
            ent.Comp.LastHeartbeat = 0;
        }
    }

    private void OnCameraData(
        Entity<SurveillanceCameraMonitorComponent> ent,
        ref SurveillanceCameraDataPayload payload,
        ref DeviceNetworkPacketData args)
    {
        var subnetData = payload.Subnet;

        if (ent.Comp.ActiveSubnet != subnetData)
        {
            DisconnectFromSubnet(ent, subnetData);
        }

        if (payload.SenderAddress != null)
            ent.Comp.KnownCameras.TryAdd(payload.SenderAddress, payload.Name);
        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnSubnetData(
        Entity<SurveillanceCameraMonitorComponent> ent,
        ref SurveillanceCameraSubnetDataPayload payload,
        ref DeviceNetworkPacketData args)
    {
        ent.Comp.KnownSubnets.TryAdd(payload.Subnet, args.SenderAddress);
        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnDisconnectMessage(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraDisconnectMessage message)
    {
        DisconnectCamera(uid, true, component);
    }

    private void OnRefreshCamerasMessage(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraRefreshCamerasMessage message)
    {
        component.KnownCameras.Clear();
        PingCameraNetwork(uid, component);
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
        TrySwitchCameraByAddress(uid, message.Address, message.CameraSubnet, component);
    }

    private void OnPowerChanged(EntityUid uid, SurveillanceCameraMonitorComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            RemoveActiveCamera(uid, component);
            component.NextCameraAddress = null;
            component.ActiveSubnet = string.Empty;
        }
    }

    private void OnShutdown(EntityUid uid, SurveillanceCameraMonitorComponent component, ComponentShutdown args)
    {
        RemoveActiveCamera(uid, component);
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
        RemoveViewer(uid, args.Actor, component);
    }

    #endregion

    private void SendHeartbeat(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || monitor.LastHeartbeatSent < _heartbeatDelay
            || string.IsNullOrEmpty(monitor.ActiveSubnet)
            || !monitor.KnownSubnets.TryGetValue(monitor.ActiveSubnet, out var subnetAddress))
        {
            return;
        }

        var payload = new SurveillanceCameraHeartbeatPayload();
        _deviceNetworkRouter.QueuePacketRouted(uid, subnetAddress, payload, monitor.ActiveCameraAddress, true);

        monitor.LastHeartbeatSent = 0;
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
        RemComp<ActiveSurveillanceCameraMonitorComponent>(uid);
        UpdateUserInterface(uid, monitor);
    }

    private void RefreshSubnets(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        monitor.KnownSubnets.Clear();
        PingSubnets(uid, monitor);
    }

    private void PingCameraNetwork(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        var payload = new SurveillanceCameraPingPayload
        {
            Subnet = monitor.ActiveSubnet,
        };
        _deviceNetworkRouter.QueuePacketRouted(uid, null, payload, null, true);
    }

    private void SetActiveSubnet(EntityUid uid, string subnet,
        SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || string.IsNullOrEmpty(subnet)
            || !monitor.KnownSubnets.ContainsKey(subnet))
        {
            return;
        }

        DisconnectFromSubnet(uid, monitor.ActiveSubnet);
        DisconnectCamera(uid, true, monitor);
        monitor.ActiveSubnet = subnet;
        monitor.KnownCameras.Clear();
        UpdateUserInterface(uid, monitor);

        ConnectToSubnet(uid, subnet);
    }

    private void PingSubnets(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        var payload = new SurveillanceCameraPingSubnetPayload();
        _deviceNetworkSystem.QueuePacket(uid, null, payload);
    }

    private void ConnectToSubnet(EntityUid uid, string subnet, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || string.IsNullOrEmpty(subnet)
            || !monitor.KnownSubnets.TryGetValue(subnet, out var address))
        {
            return;
        }

        var payload = new SurveillanceCameraSubnetConnectPayload();
        _deviceNetworkSystem.QueuePacket(uid, address, payload);

        PingSubnets(uid);
    }

    private void DisconnectFromSubnet(EntityUid uid, string subnet, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || string.IsNullOrEmpty(subnet)
            || !monitor.KnownSubnets.TryGetValue(subnet, out var address))
        {
            return;
        }

        var payload = new SurveillanceCameraSubnetDisconnectPayload();
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

    private void TrySwitchCameraByAddress(EntityUid uid, string address, string? cameraSubnet = null, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
            return;

        if (cameraSubnet != null && cameraSubnet != monitor.ActiveSubnet)
            SetActiveSubnet(uid, cameraSubnet, monitor);

        var activeSubnet = monitor.ActiveSubnet;

        if (string.IsNullOrEmpty(activeSubnet) || !monitor.KnownSubnets.TryGetValue(activeSubnet, out var subnetAddress))
            return;

        var payload = new SurveillanceCameraConnectRequestPayload();
        monitor.NextCameraAddress = address;
        _deviceNetworkRouter.QueuePacketRouted(uid, subnetAddress, payload, address, true);
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

        var state = new SurveillanceCameraMonitorUiState(GetNetEntity(monitor.ActiveCamera), monitor.KnownSubnets.Keys.ToHashSet(), monitor.ActiveCameraAddress, monitor.ActiveSubnet, monitor.KnownCameras);
        _userInterface.SetUiState(uid, SurveillanceCameraMonitorUiKey.Key, state);
    }
}
