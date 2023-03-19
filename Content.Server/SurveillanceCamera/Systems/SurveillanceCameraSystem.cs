using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DeviceNetwork;
using Content.Shared.SurveillanceCamera;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    // Pings a surveillance camera subnet. All cameras will always respond
    // with a data message if they are on the same subnet.
    public const string CameraPingSubnetMessage = "surveillance_camera_ping_subnet";

    // Pings a surveillance camera. Useful to ensure that the camera is still on
    // before connecting fully.
    public const string CameraPingMessage = "surveillance_camera_ping";

    // Camera heartbeat. Monitors ping this to ensure that a camera is still able to
    // be contacted. If this doesn't get sent after some time, the monitor will
    // automatically disconnect.
    public const string CameraHeartbeatMessage = "surveillance_camera_heartbeat";

    // Surveillance camera data. This generally should contain nothing
    // except for the subnet that this camera is on -
    // this is because of the fact that the PacketEvent already
    // contains the sender UID, and that this will always be targeted
    // towards the sender that pinged the camera.
    public const string CameraDataMessage = "surveillance_camera_data";
    public const string CameraConnectMessage = "surveillance_camera_connect";
    public const string CameraSubnetConnectMessage = "surveillance_camera_subnet_connect";
    public const string CameraSubnetDisconnectMessage = "surveillance_camera_subnet_disconnect";

    public const string CameraAddressData = "surveillance_camera_data_origin";
    public const string CameraNameData = "surveillance_camera_data_name";
    public const string CameraSubnetData = "surveillance_camera_data_subnet";

    public const int CameraNameLimit = 32;

    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SurveillanceCameraComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SurveillanceCameraComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<SurveillanceCameraComponent, SurveillanceCameraSetupSetName>(OnSetName);
        SubscribeLocalEvent<SurveillanceCameraComponent, SurveillanceCameraSetupSetNetwork>(OnSetNetwork);
        SubscribeLocalEvent<SurveillanceCameraComponent, GetVerbsEvent<AlternativeVerb>>(AddVerbs);
    }

    private void OnPacketReceived(EntityUid uid, SurveillanceCameraComponent component, DeviceNetworkPacketEvent args)
    {
        if (!component.Active)
        {
            return;
        }

        if (!TryComp(uid, out DeviceNetworkComponent? deviceNet))
        {
            return;
        }

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            var payload = new NetworkPayload()
            {
                { DeviceNetworkConstants.Command, string.Empty },
                { CameraAddressData, deviceNet.Address },
                { CameraNameData, component.CameraId },
                { CameraSubnetData, string.Empty }
            };

            var dest = string.Empty;

            switch (command)
            {
                case CameraConnectMessage:
                    if (!args.Data.TryGetValue(CameraAddressData, out dest)
                        || string.IsNullOrEmpty(args.Address))
                    {
                        return;
                    }

                    payload[DeviceNetworkConstants.Command] = CameraConnectMessage;
                    break;
                case CameraHeartbeatMessage:
                    if (!args.Data.TryGetValue(CameraAddressData, out dest)
                        || string.IsNullOrEmpty(args.Address))
                    {
                        return;
                    }

                    payload[DeviceNetworkConstants.Command] = CameraHeartbeatMessage;
                    break;
                case CameraPingMessage:
                    if (!args.Data.TryGetValue(CameraSubnetData, out string? subnet))
                    {
                        return;
                    }

                    dest = args.SenderAddress;
                    payload[CameraSubnetData] = subnet;
                    payload[DeviceNetworkConstants.Command] = CameraDataMessage;
                    break;
            }

            _deviceNetworkSystem.QueuePacket(
                uid,
                dest,
                payload);
        }
    }

    private void AddVerbs(EntityUid uid, SurveillanceCameraComponent component, GetVerbsEvent<AlternativeVerb> verbs)
    {
        if (!_actionBlocker.CanInteract(verbs.User, uid))
        {
            return;
        }

        if (component.NameSet && component.NetworkSet)
        {
            return;
        }

        AlternativeVerb verb = new();
        verb.Text = Loc.GetString("surveillance-camera-setup");
        verb.Act = () => OpenSetupInterface(uid, verbs.User, component);
        verbs.Verbs.Add(verb);
    }



    private void OnPowerChanged(EntityUid camera, SurveillanceCameraComponent component, ref PowerChangedEvent args)
    {
        SetActive(camera, args.Powered, component);
    }

    private void OnShutdown(EntityUid camera, SurveillanceCameraComponent component, ComponentShutdown args)
    {
        Deactivate(camera, component);
    }

    private void OnSetName(EntityUid uid, SurveillanceCameraComponent component, SurveillanceCameraSetupSetName args)
    {
        if (args.UiKey is not SurveillanceCameraSetupUiKey key
            || key != SurveillanceCameraSetupUiKey.Camera
            || string.IsNullOrEmpty(args.Name)
            || args.Name.Length > CameraNameLimit)
        {
            return;
        }

        component.CameraId = args.Name;
        component.NameSet = true;
        UpdateSetupInterface(uid, component);
    }

    private void OnSetNetwork(EntityUid uid, SurveillanceCameraComponent component,
        SurveillanceCameraSetupSetNetwork args)
    {
        if (args.UiKey is not SurveillanceCameraSetupUiKey key
            || key != SurveillanceCameraSetupUiKey.Camera)
        {
            return;
        }
        if (args.Network < 0 || args.Network >= component.AvailableNetworks.Count)
        {
            return;
        }

        if (!_prototypeManager.TryIndex<DeviceFrequencyPrototype>(component.AvailableNetworks[args.Network],
                out var frequency))
        {
            return;
        }

        _deviceNetworkSystem.SetReceiveFrequency(uid, frequency.Frequency);
        component.NetworkSet = true;
        UpdateSetupInterface(uid, component);
    }

    private void OpenSetupInterface(EntityUid uid, EntityUid player, SurveillanceCameraComponent? camera = null, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref camera)
            || !Resolve(player, ref actor))
        {
            return;
        }

        _userInterface.GetUiOrNull(uid, SurveillanceCameraSetupUiKey.Camera)!.Open(actor.PlayerSession);
        UpdateSetupInterface(uid, camera);
    }

    private void UpdateSetupInterface(EntityUid uid, SurveillanceCameraComponent? camera = null, DeviceNetworkComponent? deviceNet = null)
    {
        if (!Resolve(uid, ref camera, ref deviceNet))
        {
            return;
        }

        if (camera.NameSet && camera.NetworkSet)
        {
            _userInterface.TryCloseAll(uid, SurveillanceCameraSetupUiKey.Camera);
            return;
        }

        if (camera.AvailableNetworks.Count == 0)
        {
            if (deviceNet.ReceiveFrequencyId != null)
            {
                camera.AvailableNetworks.Add(deviceNet.ReceiveFrequencyId);
            }
            else if (!camera.NetworkSet)
            {
                _userInterface.TryCloseAll(uid, SurveillanceCameraSetupUiKey.Camera);
                return;
            }
        }

        var state = new SurveillanceCameraSetupBoundUiState(camera.CameraId, deviceNet.ReceiveFrequency ?? 0,
            camera.AvailableNetworks, camera.NameSet, camera.NetworkSet);
        _userInterface.TrySetUiState(uid, SurveillanceCameraSetupUiKey.Camera, state);
    }

    // If the camera deactivates for any reason, it must have all viewers removed,
    // and the relevant event broadcast to all systems.
    private void Deactivate(EntityUid camera, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        var ev = new SurveillanceCameraDeactivateEvent(camera);

        RemoveActiveViewers(camera, new(component.ActiveViewers), null, component);
        component.Active = false;

        // Send a targetted event to all monitors.
        foreach (var monitor in component.ActiveMonitors)
        {
            RaiseLocalEvent(monitor, ev, true);
        }

        component.ActiveMonitors.Clear();

        // Send a local event that's broadcasted everywhere afterwards.
        RaiseLocalEvent(ev);

        UpdateVisuals(camera, component);
    }

    public void SetActive(EntityUid camera, bool setting, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        if (setting)
        {
            component.Active = setting;
        }
        else
        {
            Deactivate(camera, component);
        }

        UpdateVisuals(camera, component);
    }

    public void AddActiveViewer(EntityUid camera, EntityUid player, EntityUid? monitor = null, SurveillanceCameraComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(camera, ref component)
            || !component.Active
            || !Resolve(player, ref actor))
        {
            return;
        }

        _viewSubscriberSystem.AddViewSubscriber(camera, actor.PlayerSession);
        component.ActiveViewers.Add(player);

        if (monitor != null)
        {
            component.ActiveMonitors.Add(monitor.Value);
        }

        UpdateVisuals(camera, component);
    }

    public void AddActiveViewers(EntityUid camera, HashSet<EntityUid> players, EntityUid? monitor = null, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component) || !component.Active)
        {
            return;
        }

        foreach (var player in players)
        {
            AddActiveViewer(camera, player, monitor, component);
        }
    }

    // Switch the set of active viewers from one camera to another.
    public void SwitchActiveViewers(EntityUid oldCamera, EntityUid newCamera, HashSet<EntityUid> players, EntityUid? monitor = null, SurveillanceCameraComponent? oldCameraComponent = null, SurveillanceCameraComponent? newCameraComponent = null)
    {
        if (!Resolve(oldCamera, ref oldCameraComponent)
            || !Resolve(newCamera, ref newCameraComponent)
            || !oldCameraComponent.Active
            || !newCameraComponent.Active)
        {
            return;
        }

        if (monitor != null)
        {
            oldCameraComponent.ActiveMonitors.Remove(monitor.Value);
            newCameraComponent.ActiveMonitors.Add(monitor.Value);
        }

        foreach (var player in players)
        {
            RemoveActiveViewer(oldCamera, player, null, oldCameraComponent);
            AddActiveViewer(newCamera, player, null, newCameraComponent);
        }
    }

    public void RemoveActiveViewer(EntityUid camera, EntityUid player, EntityUid? monitor = null, SurveillanceCameraComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(camera, ref component)
            || !Resolve(player, ref actor))
        {
            return;
        }

        _viewSubscriberSystem.RemoveViewSubscriber(camera, actor.PlayerSession);
        component.ActiveViewers.Remove(player);

        if (monitor != null)
        {
            component.ActiveMonitors.Remove(monitor.Value);
        }

        UpdateVisuals(camera, component);
    }

    public void RemoveActiveViewers(EntityUid camera, HashSet<EntityUid> players, EntityUid? monitor = null, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        foreach (var player in players)
        {
            RemoveActiveViewer(camera, player, monitor, component);
        }
    }

    private void UpdateVisuals(EntityUid uid, SurveillanceCameraComponent? component = null, AppearanceComponent? appearance = null)
    {
        // Don't log missing, because otherwise tests fail.
        if (!Resolve(uid, ref component, ref appearance, false))
        {
            return;
        }

        var key = SurveillanceCameraVisuals.Disabled;

        if (component.Active)
        {
            key = SurveillanceCameraVisuals.Active;
        }

        if (component.ActiveViewers.Count > 0 || component.ActiveMonitors.Count > 0)
        {
            key = SurveillanceCameraVisuals.InUse;
        }

        _appearance.SetData(uid, SurveillanceCameraVisualsKey.Key, key, appearance);
    }
}

public sealed class OnSurveillanceCameraViewerAddEvent : EntityEventArgs
{

}

public sealed class OnSurveillanceCameraViewerRemoveEvent : EntityEventArgs
{

}

// What happens when a camera deactivates.
public sealed class SurveillanceCameraDeactivateEvent : EntityEventArgs
{
    public EntityUid Camera { get; }

    public SurveillanceCameraDeactivateEvent(EntityUid camera)
    {
        Camera = camera;
    }
}
