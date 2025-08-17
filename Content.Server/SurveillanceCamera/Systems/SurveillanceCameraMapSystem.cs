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
        SubscribeLocalEvent<SurveillanceCameraComponent, ComponentStartup>(OnCameraStartup);
        SubscribeLocalEvent<SurveillanceCameraComponent, MoveEvent>(OnCameraMoved);

        SubscribeNetworkEvent<RequestCameraMarkerUpdateMessage>(OnRequestCameraMarkerUpdate);
    }
    private void OnCameraStartup(EntityUid uid, SurveillanceCameraComponent comp, ComponentStartup args)
    {
        UpdateCameraMarker((uid, comp));
    }

    private void OnCameraMoved(EntityUid uid, SurveillanceCameraComponent comp, ref MoveEvent args)
    {
        var oldGridUid = _transform.GetGrid(args.OldPosition);
        var newGridUid = _transform.GetGrid(args.NewPosition);

        if (oldGridUid != newGridUid && oldGridUid is not null)
        {
            if (TryComp<SurveillanceCameraMapComponent>(oldGridUid, out var oldMapComp))
            {
                var netEntity = GetNetEntity(uid);
                if (oldMapComp.Cameras.Remove(netEntity))
                    Dirty(oldGridUid.Value, oldMapComp);
            }
        }

        if (newGridUid is not null)
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

        if (mapComp.Cameras.TryGetValue(netEntity, out var existing) &&
            existing.Position.Equals(localPos) &&
            existing.Active == active &&
            existing.Address == address &&
            existing.Subnet == subnet)
        {
            return;
        }

        mapComp.Cameras[netEntity] = new CameraMarker
        {
            Position = localPos,
            Active = active,
            Address = address,
            Subnet = subnet
        };
        Dirty(gridUid.Value, mapComp);
    }
}
