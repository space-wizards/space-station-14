using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.SurveillanceCamera;
using Robust.Server.GameObjects;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraSystem : SharedSurveillanceCameraSystem
{
    [Dependency] private ViewSubscriberSystem _viewSubscriberSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

    // Pings a surveillance camera subnet. All cameras will always respond
    // with a data message if they are on the same subnet.
    public const string CameraPingSubnetMessage = "surveillance_camera_ping_subnet";

    // Pings a surveillance camera. Useful to ensure that the camera is still on
    // before connecting fully.
    public const string CameraPingMessage = "surveillance_camera_ping";

    // Surveillance camera data. This generally should contain nothing
    // except for the subnet that this camera is on -
    // this is because of the fact that the PacketEvent already
    // contains the sender UID, and that this will always be targeted
    // towards the sender that pinged the camera.
    public const string CameraDataMessage = "surveillance_camera_data";

    public const string CameraNameData = "surveillance_camera_data_name";
    public const string CameraSubnetData = "surveillance_camera_data_subnet";

    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SurveillanceCameraComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SurveillanceCameraComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnPacketReceived(EntityUid uid, SurveillanceCameraComponent component, DeviceNetworkPacketEvent args)
    {
        // no broadcast allowed
        if (args.Address == null)
        {
            return;
        }

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, CameraDataMessage },
            { CameraNameData, component.Id },
            { CameraSubnetData, component.Subnet }
        };

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            switch (command)
            {
                case CameraPingMessage:
                case CameraPingSubnetMessage:
                    _deviceNetworkSystem.QueuePacket(
                        uid,
                        args.Address,
                        payload);
                    break;
            }
        }
    }

    private void OnPowerChanged(EntityUid camera, SurveillanceCameraComponent component, PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            Deactivate(camera, component);
        }
    }

    private void OnShutdown(EntityUid camera, SurveillanceCameraComponent component, ComponentShutdown args)
    {
        Deactivate(camera, component);
    }

    // If the camera deactivates for any reason, it must have all viewers removed,
    // and the relevant event broadcast to all systems.
    private void Deactivate(EntityUid camera, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        RemoveActiveViewers(camera, new(component.ActiveViewers), component);
        RaiseLocalEvent(new SurveillanceCameraDeactivateEvent(camera));
    }

    public void AddActiveViewer(EntityUid camera, EntityUid player, SurveillanceCameraComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(camera, ref component)
            || !Resolve(player, ref actor))
        {
            return;
        }

        _viewSubscriberSystem.AddViewSubscriber(camera, actor.PlayerSession);
        component.ActiveViewers.Add(player);
    }

    public void AddActiveViewers(EntityUid camera, HashSet<EntityUid> players, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        foreach (var player in players)
        {
            AddActiveViewer(camera, player, component);
        }
    }

    // Switch the set of active viewers from one camera to another.
    public void SwitchActiveViewers(EntityUid oldCamera, EntityUid newCamera, HashSet<EntityUid> players, SurveillanceCameraComponent? oldCameraComponent = null, SurveillanceCameraComponent? newCameraComponent = null)
    {
        if (!Resolve(oldCamera, ref oldCameraComponent)
            || !Resolve(newCamera, ref newCameraComponent))
        {
            return;
        }

        foreach (var player in players)
        {
            RemoveActiveViewer(oldCamera, player, oldCameraComponent);
            AddActiveViewer(newCamera, player, newCameraComponent);
        }
    }

    public void RemoveActiveViewer(EntityUid camera, EntityUid player, SurveillanceCameraComponent? component = null, ActorComponent? actor = null)
    {
        if (!Resolve(camera, ref component)
            || !Resolve(player, ref actor))
        {
            return;
        }

        _viewSubscriberSystem.RemoveViewSubscriber(camera, actor.PlayerSession);
        component.ActiveViewers.Remove(player);
    }

    public void RemoveActiveViewers(EntityUid camera, HashSet<EntityUid> players, SurveillanceCameraComponent? component = null)
    {
        if (!Resolve(camera, ref component))
        {
            return;
        }

        foreach (var player in players)
        {
            RemoveActiveViewer(camera, player, component);
        }
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
