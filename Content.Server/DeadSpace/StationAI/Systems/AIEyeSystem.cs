using Content.Server.SurveillanceCamera;
using Robust.Server.GameObjects;
using Content.Server.Popups;
using Content.Shared.Silicons.StationAi;
using Content.Shared.DeadSpace.StationAi;
using Robust.Shared.Containers;

namespace Content.Server.DeadSpace.StationAI;

public sealed class AiEyeSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiEyeComponent, EyeMoveToCam>(OnMoveToCam);
        SubscribeLocalEvent<AiEyeComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, AiEyeComponent component, ComponentStartup args)
    {
        var eyeGrid = Transform(uid).GridUid;
        var cameras = EntityQueryEnumerator<SurveillanceCameraComponent, TransformComponent>();

        while (cameras.MoveNext(out var camUid, out _, out var transformComponent))
        {
            if (transformComponent.GridUid != eyeGrid)
                continue;

            component.Cameras.Add((GetNetEntity(camUid), GetNetCoordinates(transformComponent.Coordinates)));
        }

        Dirty(uid, component);
    }

    private void OnMoveToCam(Entity<AiEyeComponent> ent, ref EyeMoveToCam args)
    {
        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return;

        if (!TryComp<StationAiCoreComponent>(container.Owner, out var core))
            return;

        if (core.RemoteEntity == null)
            return;

        var eye = core.RemoteEntity.Value;

        if (!TryGetEntity(args.Uid, out var camera))
            return;

        var camPos = Transform(camera.Value);

        if (Transform(camera.Value).GridUid != Transform(eye).GridUid)
            return;

        if (!TryComp<SurveillanceCameraComponent>(camera, out var cameraComp))
            return;

        if (!cameraComp.Active)
        {
            _popup.PopupCursor("Камера не работает!", eye, Shared.Popups.PopupType.LargeCaution);
            return;
        }

        _transform.SetCoordinates(eye, camPos.Coordinates);
        _transform.AttachToGridOrMap(eye);
    }
}
