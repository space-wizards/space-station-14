using System.Numerics;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraMapSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PowerReceiverSystem _power = default!;

    [SubscribeLocalEvent(after: [typeof(DeviceNetworkSystem)])]
    private void OnCameraInit(Entity<SurveillanceCameraComponent> ent, ref MapInitEvent args)
    {
        UpdateCameraMarker(ent);
    }

    [SubscribeLocalEvent]
    private void OnCameraMoved(Entity<SurveillanceCameraComponent> ent, ref MoveEvent args)
    {
        if (!args.ParentChanged)
        {
            UpdateCameraMarker(ent);
            return;
        }

        var oldGridUid = _transform.GetGrid(args.OldPosition);
        var newGridUid = args.Component.GridUid;

        if (oldGridUid != newGridUid && oldGridUid is not null && !TerminatingOrDeleted(oldGridUid.Value))
        {
            if (TryComp<SurveillanceCameraMapComponent>(oldGridUid, out var oldMapComp))
            {
                var netEntity = GetNetEntity(ent.Owner);
                if (oldMapComp.Cameras.Remove(netEntity))
                    Dirty(oldGridUid.Value, oldMapComp);
            }
        }

        if (newGridUid is not null && !TerminatingOrDeleted(newGridUid.Value))
            UpdateCameraMarker(ent);
    }

    [SubscribeLocalEvent]
    private void OnFrequencyChanged(Entity<SurveillanceCameraComponent> ent, ref DeviceReceiveFrequencyChangedEvent args)
    {
        UpdateCameraMarker(ent);
    }

    [SubscribeLocalEvent]
    private void OnAddressChanged(Entity<SurveillanceCameraComponent> ent, ref DeviceAddressChangedEvent args)
    {
        UpdateCameraMarker(ent);
    }

    [SubscribeNetworkEvent]
    private void OnRequestCameraMarkerUpdate(RequestCameraMarkerUpdateMessage args)
    {
        var cameraEntity = GetEntity(args.CameraEntity);

        if (TryComp<SurveillanceCameraComponent>(cameraEntity, out var comp)
            && HasComp<DeviceNetworkComponent>(cameraEntity))
            UpdateCameraMarker((cameraEntity, comp));
    }

    /// <summary>
    /// Updates camera data in the <see cref="SurveillanceCameraMapComponent"/> for the specified camera.
    /// </summary>
    public void UpdateCameraMarker(Entity<SurveillanceCameraComponent> camera)
    {
        var (uid, comp) = camera;

        if (TerminatingOrDeleted(uid))
            return;

        if (!TryComp(uid, out TransformComponent? xform) || !TryComp(uid, out DeviceNetworkComponent? deviceNet))
            return;

        var gridUid = xform.GridUid;
        if (gridUid is null)
            return;

        if (deviceNet.ReceiveFrequencyId == null)
            return;

        var netEntity = GetNetEntity(uid);

        var mapComp = EnsureComp<SurveillanceCameraMapComponent>(gridUid.Value);
        var worldPos = _transform.GetWorldPosition(xform);
        var gridMatrix = _transform.GetInvWorldMatrix(Transform(gridUid.Value));
        var localPos = Vector2.Transform(worldPos, gridMatrix);

        var address = deviceNet.Address;
        var subnet = deviceNet.ReceiveFrequencyId.Value;
        var powered = _power.IsPowered(uid);
        var active = comp.Active && powered;

        var exists = mapComp.Cameras.TryGetValue(netEntity, out var existing);

        if (exists &&
            existing.Position.Equals(localPos) &&
            existing.Active == active &&
            existing.Address == address &&
            existing.Subnet == subnet)
        {
            return;
        }

        var visible = !exists || existing.Visible;

        mapComp.Cameras[netEntity] = new CameraMarker
        {
            Position = localPos,
            Active = active,
            Address = address,
            Subnet = subnet,
            Visible = visible,
        };
        Dirty(gridUid.Value, mapComp);
    }

    /// <summary>
    /// Sets the visibility state of the camera on the camera map.
    /// </summary>
    public void SetCameraVisibility(EntityUid cameraUid, bool visible)
    {
        if (!TryComp(cameraUid, out TransformComponent? xform))
            return;

        var gridUid = xform.GridUid;
        if (gridUid is null || !TryComp<SurveillanceCameraMapComponent>(gridUid.Value, out var mapComp))
            return;

        var netEntity = GetNetEntity(cameraUid);
        if (!mapComp.Cameras.TryGetValue(netEntity, out var marker)
            || marker.Visible == visible)
            return;

        marker.Visible = visible;
        mapComp.Cameras[netEntity] = marker;
        Dirty(gridUid.Value, mapComp);
    }

    /// <summary>
    /// Checks if the camera is currently visible on the camera map.
    /// </summary>
    public bool IsCameraVisible(EntityUid cameraUid)
    {
        if (!TryComp(cameraUid, out TransformComponent? xform))
            return false;

        var gridUid = xform.GridUid;
        if (gridUid is null || !TryComp<SurveillanceCameraMapComponent>(gridUid, out var mapComp))
            return false;

        var netEntity = GetNetEntity(cameraUid);
        return mapComp.Cameras.TryGetValue(netEntity, out var marker) && marker.Visible;
    }
}
