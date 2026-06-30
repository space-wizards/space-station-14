using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraSystem : SharedSurveillanceCameraSystem
{
    [Dependency] private ViewSubscriberSystem _viewSubscriberSystem = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private DeviceNetworkRouterSystem _deviceNetworkRouter = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SurveillanceCameraMapSystem _cameraMapSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private EntityQuery<SurveillanceCameraRouterComponent> _routerQuery = default!;

    public const int CameraNameLimit = 32;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceCameraComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SurveillanceCameraComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SurveillanceCameraComponent, SurveillanceCameraSetupSetName>(OnSetName);
        SubscribeLocalEvent<SurveillanceCameraComponent, SurveillanceCameraSetupSetNetwork>(OnSetNetwork);

        InitializeCollide();
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<SurveillanceCameraConnectRequestPayload>(OnConnectRequest);
        SubscribePayload<SurveillanceCameraHeartbeatRequestPayload>(OnHeartbeatRequest);
        SubscribePayload<SurveillanceCameraPingPayload>(OnPing);
    }

    private void OnConnectRequest(
        Entity<SurveillanceCameraComponent> ent,
        ref SurveillanceCameraConnectRequestPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (!ent.Comp.Active)
            return;

        var responsePayload = new SurveillanceCameraConnectPayload();
        _deviceNetworkRouter.QueuePacketRouted(ent.Owner, args.SenderAddress, responsePayload, payload.SenderAddress);
    }

    private void OnHeartbeatRequest(
        Entity<SurveillanceCameraComponent> ent,
        ref SurveillanceCameraHeartbeatRequestPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (!ent.Comp.Active)
            return;

        var responsePayload = new SurveillanceCameraHeartbeatPayload();
        _deviceNetworkRouter.QueuePacketRouted(ent.Owner, args.SenderAddress, responsePayload, payload.SenderAddress);
    }

    private void OnPing(
        Entity<SurveillanceCameraComponent> ent,
        ref SurveillanceCameraPingPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (!ent.Comp.Active)
            return;

        if (!_routerQuery.TryComp(args.Sender, out var routerComp))
            return;

        if (routerComp.SubnetName != payload.Subnet)
            return;

        var name = ent.Comp.UseEntityNameAsCameraId ? MetaData(ent).EntityName : ent.Comp.CameraId;
        var responsePayload = new SurveillanceCameraDataPayload
        {
            Name = name,
        };
        _deviceNetworkRouter.QueuePacketRouted(ent.Owner, args.SenderAddress, responsePayload, payload.SenderAddress);
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
        Dirty(uid, component);
        UpdateSetupInterface(uid, component);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(args.Actor)} set the name of {ToPrettyString(uid)} to \"{args.Name}.\"");
    }

    private void OnSetNetwork(Entity<SurveillanceCameraComponent> ent, ref SurveillanceCameraSetupSetNetwork args)
    {
        var (uid, component) = ent;
        if (args.UiKey is not SurveillanceCameraSetupUiKey key
            || key != SurveillanceCameraSetupUiKey.Camera)
        {
            return;
        }
        if (args.Network < 0 || args.Network >= component.AvailableNetworks.Count)
        {
            return;
        }

        if (!ProtoMan.Resolve(component.AvailableNetworks[args.Network], out var frequency))
        {
            return;
        }

        _deviceNetworkSystem.SetReceiveFrequency(uid, frequency.Frequency);
        component.NetworkSet = true;
        Dirty(uid, component);
        UpdateSetupInterface(uid, component);
    }

    protected override void OpenSetupInterface(EntityUid uid, EntityUid player, SurveillanceCameraComponent? camera = null)
    {
        if (!Resolve(uid, ref camera))
            return;

        if (!_userInterface.TryOpenUi(uid, SurveillanceCameraSetupUiKey.Camera, player))
            return;

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
            _userInterface.CloseUi(uid, SurveillanceCameraSetupUiKey.Camera);
            return;
        }

        if (camera.AvailableNetworks.Count == 0)
        {
            if (deviceNet.ReceiveFrequencyId != null)
            {
                camera.AvailableNetworks.Add(deviceNet.ReceiveFrequencyId.Value);
            }
            else if (!camera.NetworkSet)
            {
                _userInterface.CloseUi(uid, SurveillanceCameraSetupUiKey.Camera);
                return;
            }
        }

        var name = camera.UseEntityNameAsCameraId ? MetaData(uid).EntityName : camera.CameraId;
        var state = new SurveillanceCameraSetupBoundUiState(name,
            deviceNet.ReceiveFrequency ?? 0,
            camera.AvailableNetworks,
            camera.NameSet,
            camera.NetworkSet);
        _userInterface.SetUiState(uid, SurveillanceCameraSetupUiKey.Camera, state);
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

        RemoveActiveViewers(camera, new(component.ActivePvsViewers), null, component);
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

    /// <summary>
    /// Checks whether the camera is being viewed through by anyone at all.
    /// </summary>
    /// <param name="ent">The camera to check</param>
    /// <returns>True if the camera is looked through, otherwise False.</returns>
    public bool IsGettingViewed(Entity<SurveillanceCameraComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.ActivePvsViewers.Count > 0 || ent.Comp.ActiveMonitors.Count > 0)
            return true;

        var ev = new SurveillanceCameraGetIsViewedExternallyEvent();
        RaiseLocalEvent(ent, ref ev);

        return ev.Viewed;
    }

    public override void SetActive(EntityUid camera, bool setting, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        if (setting)
        {
            var attemptEv = new SurveillanceCameraSetActiveAttemptEvent();
            RaiseLocalEvent(camera, ref attemptEv);
            if (attemptEv.Cancelled)
                return;
            component.Active = setting;
        }
        else
        {
            Deactivate(camera, component);
        }

        UpdateVisuals(camera, component);

        _cameraMapSystem.UpdateCameraMarker((camera, component));
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

        component.ActivePvsViewers.Add(player);

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

        // Add monitor without viewers
        if (players.Count == 0 && monitor != null)
        {
            component.ActiveMonitors.Add(monitor.Value);
            UpdateVisuals(camera, component);
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
        if (!Resolve(camera, ref component))
            return;

        if (Resolve(player, ref actor))
            _viewSubscriberSystem.RemoveViewSubscriber(camera, actor.PlayerSession);

        component.ActivePvsViewers.Remove(player);

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

        // Even if not removing any viewers, remove the monitor
        if (players.Count == 0 && monitor != null)
        {
            component.ActiveMonitors.Remove(monitor.Value);
            UpdateVisuals(camera, component);
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

        if (IsGettingViewed((uid, component)))
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

[ByRefEvent]
public record struct SurveillanceCameraSetActiveAttemptEvent(bool Cancelled);
