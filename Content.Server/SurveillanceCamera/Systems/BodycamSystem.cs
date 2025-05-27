using Content.Shared.SurveillanceCamera;

namespace Content.Server.SurveillanceCamera.Systems;

public sealed class BodycamSystem: SharedBodycamSystem
{
    [Dependency] private readonly SurveillanceCameraSystem _camera = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodycamComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, BodycamComponent comp, MapInitEvent args)
    {
        // disable the body cam on map init
        // otherwise you would be able to connect to them before they are worn
        _camera.SetActive(uid, false);
    }

    protected override void SwitchOn(EntityUid uid, BodycamComponent comp, EntityUid user)
    {
        base.SwitchOn(uid, comp, user);
        _camera.SetActive(uid, true);
        // todo: set camera name to wearer name
    }
}
