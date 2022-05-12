using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Server.Wires;
using Content.Shared.Interaction;
using Content.Shared.SurveillanceCamera;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMonitorSystem : EntitySystem
{
    [Dependency] private readonly SurveillanceCameraSystem _surveillanceCameras = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, SurveillanceCameraDeactivateEvent>(OnSurveillanceCameraDeactivate);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, BoundUIClosedEvent>(OnBoundUiClose);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, SurveillanceCameraMonitorSwitchMessage>(OnSwitchMessage);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, SurveillanceCameraMonitorSubnetRequestMessage>(OnSubnetRequest);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
    }

    // TODO:
    //
    // - What happens if a monitor is depowered?
    // - What happens if a camera is removed?
    // - Should monitors be the ones that deal in view subscriptions?
    //   (probably not!)

    // If this event is sent, the camera has already cleared the view subscriptions.
    // Deactivation can occur for any reason (deletion, power off, etc.,) but the
    // result is always the same: the monitor must update any viewers and send
    // the updated states over to viewing clients.

    #region Event Handling

    private void OnComponentStartup(EntityUid uid, SurveillanceCameraMonitorComponent component, ComponentStartup args)
    {
        PingCameraNetwork(uid, component);
    }

    private void OnSubnetRequest(EntityUid uid, SurveillanceCameraMonitorComponent component,
        SurveillanceCameraMonitorSubnetRequestMessage args)
    {
        if (args.Session.AttachedEntity != null)
        {
            SetActiveSubnet(uid, args.Subnet, component);
        }
    }

    private void OnPacketReceived(EntityUid uid, SurveillanceCameraMonitorComponent component,
        DeviceNetworkPacketEvent args)
    {
        if (args.Address == null)
        {
            return;
        }

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            switch (command)
            {
                case SurveillanceCameraSystem.CameraConnectMessage:
                    if (component.NextCameraAddress == args.Address)
                    {
                        TrySwitchCameraByUid(uid, args.Sender, component);
                    }

                    component.NextCameraAddress = null;
                    break;
                case SurveillanceCameraSystem.CameraDataMessage:
                    if (!args.Data.TryGetValue(SurveillanceCameraSystem.CameraNameData, out string? name)
                        || !args.Data.TryGetValue(SurveillanceCameraSystem.CameraSubnetData, out string? subnetData)
                        || component.ActiveSubnet != subnetData)
                    {
                        return;
                    }

                    var info = new SurveillanceCameraInfo()
                    {
                        Address = args.Address,
                        Name = name,
                        Subnet = subnetData
                    };

                    SendCameraInfoToClient(uid, info, component);

                    break;
                case SurveillanceCameraSystem.CameraPingMessage:
                    if (args.Data.TryGetValue(SurveillanceCameraSystem.CameraSubnetData, out string? subnet))
                    {
                        component.KnownSubnets.Add(subnet);
                    }

                    break;
            }
        }
    }

    private void OnSwitchMessage(EntityUid uid, SurveillanceCameraMonitorComponent component, SurveillanceCameraMonitorSwitchMessage message)
    {
        if (component.NextCameraAddress == null)
        {
            TrySwitchCameraByAddress(uid, message.Address, component);
        }
    }

    private void OnPowerChanged(EntityUid uid, SurveillanceCameraMonitorComponent component, PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            RemoveActiveCamera(uid, component);
            component.NextCameraAddress = null;
            component.ActiveSubnet = string.Empty;
        }
    }

    private void OnInteractHand(EntityUid uid, SurveillanceCameraMonitorComponent component, InteractHandEvent args)
    {
        TryOpenUserInterface(uid, args.User, component);
    }

    private void OnToggleInterface(EntityUid uid, SurveillanceCameraMonitorComponent component,
        AfterActivatableUIOpenEvent args)
    {
        TryOpenUserInterface(uid, args.User, component);
    }

    // This is to ensure that there's no delay in ensuring that a camera is deactivated.
    private void OnSurveillanceCameraDeactivate(EntityUid uid, SurveillanceCameraMonitorComponent monitor, SurveillanceCameraDeactivateEvent args)
    {
        monitor.ActiveCamera = null;
        UpdateUserInterface(uid, monitor);
    }

    private void OnBoundUiClose(EntityUid uid, SurveillanceCameraMonitorComponent component, BoundUIClosedEvent args)
    {
        RemoveViewer(uid, args.Entity, component);
    }
    #endregion

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

    private void SetActiveSubnet(EntityUid uid, string subnet,
        SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !monitor.KnownSubnets.Contains(subnet))
        {
            return;
        }

        monitor.ActiveSubnet = subnet;
        UpdateUserInterface(uid, monitor);

        RequestSubnetInfo(uid, subnet);
    }

    private void RequestSubnetInfo(EntityUid uid, string subnet, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !monitor.KnownSubnets.Contains(subnet))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            {DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraPingSubnetMessage},
            {SurveillanceCameraSystem.CameraSubnetData, subnet}
        };
        _deviceNetworkSystem.QueuePacket(uid, null, payload);
    }

    private void SendCameraInfoToClient(EntityUid uid, SurveillanceCameraInfo info,
        SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        var message = new SurveillanceCameraMonitorInfoMessage(info);

        _userInterface.TrySendUiMessage(uid,
            SurveillanceCameraMonitorUiKey.Key,
            message);
    }

    // Adds a viewer to the camera and the monitor.
    private void AddViewer(EntityUid uid, EntityUid player, SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        monitor.Viewers.Add(uid);

        if (monitor.ActiveCamera != null)
        {
            _surveillanceCameras.AddActiveViewer((EntityUid) monitor.ActiveCamera, player);
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

        monitor.Viewers.Remove(uid);

        if (monitor.ActiveCamera != null)
        {
            _surveillanceCameras.RemoveActiveViewer((EntityUid) monitor.ActiveCamera, player);
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

        _surveillanceCameras.AddActiveViewers(camera, monitor.Viewers);

        monitor.ActiveCamera = camera;

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

        _surveillanceCameras.SwitchActiveViewers((EntityUid) monitor.ActiveCamera, camera, monitor.Viewers);

        monitor.ActiveCamera = camera;

        UpdateUserInterface(uid, monitor);
    }

    private void TrySwitchCameraByAddress(EntityUid uid, string address,
        SurveillanceCameraMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            {DeviceNetworkConstants.Command, SurveillanceCameraSystem.CameraPingMessage}
        };
        monitor.NextCameraAddress = address;
        _deviceNetworkSystem.QueuePacket(uid, address, payload);
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

        _surveillanceCameras.RemoveActiveViewers((EntityUid) monitor.ActiveCamera, monitor.Viewers);

        UpdateUserInterface(uid, monitor);
    }

    // This is public primarily because it might be useful to have the ability to
    // have this component added to any entity, and have them open the BUI (somehow).
    public void TryOpenUserInterface(EntityUid uid, EntityUid player, SurveillanceCameraMonitorComponent? monitor = null, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref monitor)
            || !Resolve(player, ref actor))
        {
            return;
        }

        _userInterface.GetUiOrNull(uid, SurveillanceCameraMonitorUiKey.Key)?.Open(actor.PlayerSession);

        AddViewer(uid, player);
        RequestSubnetInfo(uid, monitor.ActiveSubnet, monitor);
    }

    private void UpdateUserInterface(EntityUid uid, SurveillanceCameraMonitorComponent? monitor = null, EntityUid? player = null)
    {
        if (!Resolve(uid, ref monitor))
        {
            return;
        }

        IPlayerSession? session = null;
        if (player != null
            && TryComp(player, out ActorComponent? actor))
        {
            session = actor.PlayerSession;
        }

        var state = new SurveillanceCameraMonitorUiState(monitor.ActiveCamera, monitor.KnownSubnets, monitor.ActiveSubnet);
        _userInterface.TrySetUiState(uid, SurveillanceCameraMonitorUiKey.Key, state);
    }
}
