using System.Numerics;
using Content.Server.Power.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.SurveillanceCamera.Components;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMapSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraComponent, MoveEvent>(OnCameraMoved);
        SubscribeLocalEvent<SurveillanceCameraComponent, EntityUnpausedEvent>(OnCameraUnpaused);

        SubscribeNetworkEvent<RequestCameraMarkerUpdateMessage>(OnRequestCameraMarkerUpdate);
    }

    private void OnCameraUnpaused(EntityUid uid, SurveillanceCameraComponent comp, ref EntityUnpausedEvent args)
    {
        if (Terminating(uid))
            return;

        UpdateCameraMarker((uid, comp));
    }

    private void OnCameraMoved(EntityUid uid, SurveillanceCameraComponent comp, ref MoveEvent args)
    {
        if (Terminating(uid))
            return;

        var oldGridUid = _transform.GetGrid(args.OldPosition);
        var newGridUid = _transform.GetGrid(args.NewPosition);

        if (oldGridUid != newGridUid && oldGridUid is not null && !Terminating(oldGridUid.Value))
        {
            if (TryComp<SurveillanceCameraMapComponent>(oldGridUid, out var oldMapComp))
            {
                var netEntity = GetNetEntity(uid);
                if (oldMapComp.Cameras.Remove(netEntity))
                    Dirty(oldGridUid.Value, oldMapComp);
            }
        }

        if (newGridUid is not null && !Terminating(newGridUid.Value))
            UpdateCameraMarker((uid, comp));
    }

    private void OnRequestCameraMarkerUpdate(RequestCameraMarkerUpdateMessage args)
    {
        var cameraEntity = GetEntity(args.CameraEntity);

        if (TryComp<SurveillanceCameraComponent>(cameraEntity, out var comp)
            && HasComp<DeviceNetworkComponent>(cameraEntity))
            UpdateCameraMarker((cameraEntity, comp));
    }

    /// <summary>
    /// Updates camera data in the SurveillanceCameraMapComponent for the specified camera entity.
    /// </summary>
    public void UpdateCameraMarker(Entity<SurveillanceCameraComponent> camera)
    {
        var (uid, comp) = camera;

        if (Terminating(uid))
            return;

        if (!TryComp(uid, out TransformComponent? xform) || !TryComp(uid, out DeviceNetworkComponent? deviceNet))
            return;

        var gridUid = xform.GridUid ?? xform.MapUid;
        if (gridUid is null)
            return;

        var netEntity = GetNetEntity(uid);

        var mapComp = EnsureComp<SurveillanceCameraMapComponent>(gridUid.Value);
        var worldPos = _transform.GetWorldPosition(xform);
        var gridMatrix = _transform.GetInvWorldMatrix(Transform(gridUid.Value));
        var localPos = Vector2.Transform(worldPos, gridMatrix);

        var address = deviceNet.Address;
        var subnet = deviceNet.ReceiveFrequencyId ?? string.Empty;
        var powered = CompOrNull<ApcPowerReceiverComponent>(uid)?.Powered ?? true;
        var active = comp.Active && powered;

        bool exists = mapComp.Cameras.TryGetValue(netEntity, out var existing);

        if (exists &&
            existing.Position.Equals(localPos) &&
            existing.Active == active &&
            existing.Address == address &&
            existing.Subnet == subnet)
        {
            return;
        }

        var visible = exists ? existing.Visible : true;

        mapComp.Cameras[netEntity] = new CameraMarker
        {
            Position = localPos,
            Active = active,
            Address = address,
            Subnet = subnet,
            Visible = visible
        };
        Dirty(gridUid.Value, mapComp);
    }

    /// <summary>
    /// Sets the visibility state of a camera on the camera map.
    /// </summary>
    public void SetCameraVisibility(EntityUid cameraUid, bool visible)
    {
        if (!TryComp(cameraUid, out TransformComponent? xform))
            return;

        var gridUid = xform.GridUid ?? xform.MapUid;
        if (gridUid == null || !TryComp<SurveillanceCameraMapComponent>(gridUid.Value, out var mapComp))
            return;

        var netEntity = GetNetEntity(cameraUid);
        if (mapComp.Cameras.TryGetValue(netEntity, out var marker))
        {
            marker.Visible = visible;
            mapComp.Cameras[netEntity] = marker;
            Dirty(gridUid.Value, mapComp);
        }
    }

    /// <summary>
    /// Checks if a camera is currently visible on the camera map.
    /// </summary>
    public bool IsCameraVisible(EntityUid cameraUid)
    {
        if (!TryComp(cameraUid, out TransformComponent? xform))
            return false;

        var gridUid = xform.GridUid ?? xform.MapUid;
        if (gridUid == null || !TryComp<SurveillanceCameraMapComponent>(gridUid, out var mapComp))
            return false;

        var netEntity = GetNetEntity(cameraUid);
        return mapComp.Cameras.TryGetValue(netEntity, out var marker) && marker.Visible;
    }
}
